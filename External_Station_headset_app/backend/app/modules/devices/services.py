from sqlalchemy.orm import Session
from datetime import datetime, timedelta
from app.models import Device 
from . import schemas
from app.state import locked_devices

def get_device_by_hash(db: Session, hardware_id: str):
    """Najde zařízení podle hashe z Unity. Použije to hlavně MQTT."""
    return db.query(Device).filter(Device.hardware_id == hardware_id).first()

def get_online_devices(db: Session):
    """Vrátí jen ta zařízení, která nespí a NIKDO JINÝ JE NESLEDUJE."""
    
    # Spočítáme si čas "teď mínus 20 vteřin"
    cutoff_time = datetime.now() - timedelta(seconds=20)
    
    # Necháme databázi najít všechny probuzené brýle 
    all_online_devices = (
        db.query(Device)
        .filter(
            Device.is_online == True,
            Device.last_seen >= cutoff_time
        )
        .all()
    )
    
    available_devices = [
        device for device in all_online_devices 
        if device.hardware_id not in locked_devices
    ]
    
    return available_devices

def create_device(db: Session, device: schemas.DeviceCreate):
    """Zaregistruje úplně nové brýle, když poprvé zařvou do discovery."""
    db_device = Device(
        hardware_id=device.hardware_id, 
        name=device.name,
        is_online=True, 
        last_seen=datetime.now()
    )
    db.add(db_device)
    db.commit()
    db.refresh(db_device)
    return db_device

def handle_device_discovery(db: Session, hardware_id: str, name: str):
    """
    Tohle zavolá MQTT. Rozhodne to, jestli se zařízení vytvoří, nebo jen aktualizuje.
    """
    device = get_device_by_hash(db, hardware_id)
    
    if not device:
        new_device_data = schemas.DeviceCreate(hardware_id=hardware_id, name=name)
        return create_device(db, new_device_data)
    else:
        device.name = name
        device.is_online = True
        device.last_seen = datetime.now()
        db.commit()
        return device
    
def handle_device_status(db: Session, hardware_id: str):
    """
    Zpracuje Heartbeat z Unity. Jen posune čas 'last_seen' na aktuální, 
    aby React věděl, že brýle stále žijí.
    """
    # Použijeme funkci pro nalezení brýlí
    device = get_device_by_hash(db, hardware_id)
    
    if device:
        device.last_seen = datetime.now()
        device.is_online = True
        db.commit()