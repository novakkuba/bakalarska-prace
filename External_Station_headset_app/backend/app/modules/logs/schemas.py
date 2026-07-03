from pydantic import BaseModel
from datetime import datetime
from typing import Dict, Any, Optional

class LogResponse(BaseModel):
    id: int
    timestamp: datetime
    topic: str

    session_id: int
    iteration: int

    
    data: Dict[str, Any] 

    class Config:
        from_attributes = True