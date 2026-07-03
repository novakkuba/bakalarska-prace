import json
from app import state 

def send_command(topic: str, payload: dict):
    """
    Sends a single command using the globally managed MQTT client.
    """
    if not state.mqtt_client or not state.mqtt_client.is_connected():
        print("PUBLISHER ERROR: MQTT client not initialized or not connected.", flush=True)
        return False

    try:
        message_str = json.dumps(payload)
        msg_info = state.mqtt_client.publish(topic, message_str) # <--- Use global client
        msg_info.wait_for_publish()
        
        print(f"📤 PUBLISHER: Sent to [{topic}]: {message_str}", flush=True)
        return True

    except Exception as e:
        print(f"PUBLISHER ERROR: {e}", flush=True)
        return False