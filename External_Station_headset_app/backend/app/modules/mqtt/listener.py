import json
from app.database import session_scope
from app.modules.logs.services import create_log_entry
from app.state import add_to_stream
from app import models


from app.modules.sessions.services import get_active_session_by_device

def on_message(client, userdata, msg):
    topic = msg.topic
    
    try:
        payload_str = msg.payload.decode("utf-8")
        data = json.loads(payload_str)
    except json.JSONDecodeError:
        print(f"❌ Chyba JSONu na topicu {topic}: {payload_str}", flush=True)
        return
    except Exception as e:
        print(f"❌ Neznámá chyba dekódování: {e}", flush=True)
        return

    # Otevřeme databázi pro tuto zprávu
    with session_scope() as db:
        
        # Discovery
        if topic == "/unitymap/discovery":
            hardware_id = data.get("hardware_id")
            short_hash = hardware_id[:4].upper()
            device_name = f"Headset {short_hash}"
            
            # Jen zavoláme tu jednu chytrou funkci z tvých services
            from app.modules.devices.services import handle_device_discovery
            handle_device_discovery(db, hardware_id, device_name)
            
            print(f"👋 DISCOVERY: Zařízení {device_name} ({hardware_id}) je připraveno!", flush=True)
            return

        
        elif topic.startswith("/unitymap/"):
            

            parts = topic.strip("/").split("/")
            
            if len(parts) == 3:
                device_id = parts[1]
                action = parts[2]

                
                print(f"🔍 VYHAZOVAČ: Zařízení={device_id}, Akce={action}", flush=True)

                # A) STATUS (Heartbeat)
                if action == "status":
                    
                    print(f"💓 STATUS: Heartbeat od {device_id} zachycen.", flush=True)
                    from app.modules.devices.services import handle_device_status
                    handle_device_status(db, device_id)

                    
                # B) LOGY
                elif action == "logs":
                    print(f"DEBUG: Přišel log z headsetu {device_id}!", flush=True)
                    
                    active_session = get_active_session_by_device(db, device_id)
                    
                    stream_package = {
                        "topic": topic,
                        "payload": data,
                        "type": "log"
                    }
                    add_to_stream(device_id, stream_package)

                    if active_session:
                        iteration = data.get("iteration", 1)
                        create_log_entry(db, data, active_session.id, topic, iteration)
                    else:
                        print(f"⚠️ Log neuložen do DB (není aktivní session), ale poslán do Reactu.")

                # C) WEBRTC
                elif action == "webrtc":
                    
                    
                    stream_package = {
                        "topic": topic,        
                        "payload": data,       
                        "type": "webrtc"       
                    }
                    
                    add_to_stream(device_id, stream_package)
                    print(f"📹 WEBRTC: Signál od {device_id} předán do streamu.", flush=True)