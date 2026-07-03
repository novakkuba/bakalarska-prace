from pydantic import BaseModel, Field
from datetime import datetime
from typing import Optional

class DeviceBase(BaseModel):
    hardware_id: str = Field(..., min_length=1, description="Unikátní hash z Unity")
    
    name: Optional[str] = "Neznámý Headset"

# 2. SCHÉMA PRO VYTVOŘENÍ
class DeviceCreate(DeviceBase):
    pass

# 3. SCHÉMA PRO ODPOVĚĎ (Z Pythonu do Reactu)
class DeviceResponse(DeviceBase):
    id: int
    last_seen: datetime
    is_online: bool

    class Config:
        from_attributes = True