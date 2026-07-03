from pydantic import BaseModel
from datetime import datetime
from typing import Optional

class SessionCreate(BaseModel):
    patient_id: int
    device_id: str  

class SessionResponse(BaseModel):
    id: int               
    patient_id: int       
    start_time: datetime
    end_time: Optional[datetime] = None
    is_active: bool


    class Config:
        from_attributes = True