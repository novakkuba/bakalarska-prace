from sqlalchemy.orm import Session as DB_Session
from app import models
from . import schemas
from datetime import datetime
from fastapi import HTTPException

def get_active_session_by_device(db: DB_Session, device_id: str): 
    """Najde, jestli na daném headsetu už neběží nějaká hra."""
    return (
        db.query(models.Session)
        .join(models.Device, models.Session.device_id == models.Device.id) 
        .filter(
            models.Device.hardware_id == device_id, 
            models.Session.is_active == True
        )
        .first()
    )

def create_session(db: DB_Session, session_data: schemas.SessionCreate):
    """Startuje nové sezení, ale hlídá, jestli pacient nebo zařízení už nejsou obsazeni."""
    
    device = db.query(models.Device).filter(models.Device.hardware_id == session_data.device_id).first()
    if not device:
        raise ValueError(f"Zařízení s hardware_id '{session_data.device_id}' neexistuje.")

    active_device_session = get_active_session_by_device(db, session_data.device_id)
    if active_device_session:
        raise HTTPException(status_code=409, detail="Toto zařízení právě používá jiný lékař.")

    active_patient_session = db.query(models.Session).filter(
        models.Session.patient_id == session_data.patient_id,
        models.Session.is_active == True
    ).first()
    
    if active_patient_session:
        raise HTTPException(status_code=409, detail="S tímto pacientem již probíhá jiná terapie na jiném zařízení.")

    new_session = models.Session(
        patient_id=session_data.patient_id,
        device_id=device.id, 
        start_time=datetime.now(),
        is_active=True
    )
    db.add(new_session)
    db.commit()
    db.refresh(new_session)
    return new_session

def end_session(db: DB_Session, session_id: int):
    """Manuální ukončení sezení doktorem z Reactu."""
    db_session = db.query(models.Session).filter(models.Session.id == session_id).first()
    if db_session:
        db_session.is_active = False
        db_session.end_time = datetime.now()
        db.commit()
        db.refresh(db_session)
    return db_session