from fastapi import APIRouter
from pydantic import BaseModel
import json

from app import state 

router = APIRouter()

class WebRTCSignal(BaseModel):
    device_id: str
    payload: dict

@router.post("/webrtc/send")
async def send_webrtc_to_unity(signal: WebRTCSignal):
    topic = f"/unitymap/{signal.device_id}/webrtc_command"
    
    print(f"🛑 ENDPOINT ZASAŽEN! Zkouším poslat na {topic}", flush=True)
    
    if state.mqtt_client:
        print(f"🚀 POSÍLÁM BUDÍČEK Z REACTU: Payload={signal.payload}", flush=True)
        state.mqtt_client.publish(topic, json.dumps(signal.payload), qos=1)
        return {"status": "success", "message": f"Signál odeslán na {topic}"}
    else:
        print("❌ CHYBA: state.mqtt_client je stále NONE!", flush=True)
        return {"status": "error", "message": "MQTT klient není připojen"}