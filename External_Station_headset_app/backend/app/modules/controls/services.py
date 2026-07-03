import json
from app.modules.mqtt.publisher import send_command

def broadcast_game_command(game_data: dict, session_id: int, device_id: str):
    """
    Vezme data, přibalí Session ID a pošle do konkrétních brýlí Unity.
    """
    # 1. Dynamicky vytvoříme dálnici pro tyto konkrétní brýle
    target_topic = f"/unitymap/{device_id}/command"
    
    mqtt_payload = {
        "session_id": session_id,      
        "game": game_data["game_type"],
        "config": game_data,           
        "msg": "Command from Operator Station"
    }
    
    return send_command(target_topic, mqtt_payload)