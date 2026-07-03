import asyncio
import json
import queue
from fastapi import APIRouter, Request
from sse_starlette.sse import EventSourceResponse

# Importujeme náš slovník místo get_from_stream
from app.state import active_queues, locked_devices

router = APIRouter()
STREAM_DELAY = 0.05


@router.get("/stream/{device_id}")
async def message_stream(request: Request, device_id: str):
    """
    Dynamický stream. Vytvoří osobní kanál pro konkrétní zařízení (brýle).
    """
    
    # 1. PŘÍPRAVA: Vytvoříme frontu pro toto zařízení, pokud ještě neexistuje
    if device_id not in active_queues:
        active_queues[device_id] = queue.Queue()
        print(f"📦 Vytvořena nová streamovací fronta pro zařízení {device_id}")
    
    my_queue = active_queues[device_id]

    async def event_generator():
        
        locked_devices.add(device_id)
        print(f"🔒 Zařízení {device_id} je nyní obsazeno (locked).")

        try:
            while True:
                # 1. SAFETY CHECK
                if await request.is_disconnected():
                    print(f"🛑 Klient se odpojil od streamu {device_id}")
                    break

                # 2. CHECK THE MAILBOX
                try:
                    data = my_queue.get_nowait()
                    yield json.dumps(data)
                except queue.Empty:
                    pass
                
                await asyncio.sleep(STREAM_DELAY)
        
        finally:
            # 1. Odemkneme brýle z paměti pro ostatní 
            if device_id in locked_devices:
                locked_devices.remove(device_id)
            
            # 2. Úklid fronty
            if device_id in active_queues:
                del active_queues[device_id]
            
            from app.database import SessionLocal 
            from app import models # Uprav podle své struktury importů
            from datetime import datetime
            
            db = SessionLocal()
            try:
                
                device = db.query(models.Device).filter(models.Device.hardware_id == device_id).first()
                
                if device:
                    # Krok B: Najdeme aktivní session POUZE pro toto jedno zařízení
                    active_session = db.query(models.Session).filter(
                        models.Session.device_id == device.id, 
                        models.Session.is_active == True
                    ).first()
                    
                    if active_session:
                        active_session.is_active = False
                        active_session.end_time = datetime.now()
                        db.commit()
                        print(f"💀 Session pro {device.name} byla automaticky ukončena po pádu prohlížeče.")
            finally:
                # Vždy musíme zavřít spojení s DB
                db.close()

    return EventSourceResponse(event_generator())