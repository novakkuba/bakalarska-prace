from fastapi import APIRouter, Depends, HTTPException
from sqlalchemy.orm import Session
from app.database import get_db
from . import schemas, services

router = APIRouter(tags=["Sessions"])

@router.post("/", response_model=schemas.SessionResponse)
def start_session(session_in: schemas.SessionCreate, db: Session = Depends(get_db)):
    """
    ENDPOINT: Start hry.
    Vezme ID pacienta a ID headsetu a vrátí nové session_id.
    """
    return services.create_session(db, session_in)

@router.post("/{session_id}/end", response_model=schemas.SessionResponse)
def stop_session(session_id: int, db: Session = Depends(get_db)):
    """
    ENDPOINT: Konec hry.
    Uzavře sezení v databázi.
    """
    ended = services.end_session(db, session_id)
    if not ended:
        raise HTTPException(status_code=404, detail="Sezení nenalezeno.")
    return ended