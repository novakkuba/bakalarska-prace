from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session
from typing import List

from app.database import get_db
from . import services, schemas


router = APIRouter()

@router.get("/", response_model=List[schemas.LogResponse])
def read_logs(limit: int = 100, db: Session = Depends(get_db)):
    """
    API Endpoint: GET /api/logs
    Returns a list of the most recent events.
    """
    # We call the service, passing the database session
    logs = services.get_all_logs(db, limit=limit)
    return logs