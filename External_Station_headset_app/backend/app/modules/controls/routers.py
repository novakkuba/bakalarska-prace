
from fastapi import APIRouter, HTTPException                              
from .schemas import GameCommand
from .services import broadcast_game_command

router = APIRouter(prefix="/api/game", tags=["Controls"])

@router.get("/schema")
async def get_game_schema():
    """
    (Tohle zůstává stejné - vrací formuláře pro React)
    """
    schema = GameCommand.model_json_schema()
    return schema

@router.post("/command")
async def send_command_to_unity(
    command: GameCommand, 
    session_id: int,  
    device_id: str,   
):
    # Pydantic model na slovník
    game_data = command.payload.dict()
    
    # Jen to vystřelíme do brýlí
    success = broadcast_game_command(game_data, session_id, device_id)
    
    if not success:
        raise HTTPException(status_code=500, detail="MQTT Broker unreachable")
        
    return {"status": "success", "session_id": session_id, "device_id": device_id}