from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session
from typing import List
from app.models import Device 
from app.database import get_db 
from . import schemas, services

router = APIRouter(
    prefix="/devices",
    tags=["Devices"]
)

@router.get("/online", response_model=List[schemas.DeviceResponse])
def get_online_devices(db: Session = Depends(get_db)):
    """
    Vrátí všechny připojené brýle. 
    React si to zavolá pro naplnění roletky při výběru headsetu.
    """
    return services.get_online_devices(db)

@router.get("/")
def get_all_devices(db: Session = Depends(get_db)):
    """
    DEBUG: Vrátí úplně všechna zařízení v databázi, bez ohledu na to, zda jsou online.
    """
    devices = db.query(Device).all()
    return devices