from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session
from typing import List, Optional

from app.database import get_db 
from . import schemas, services

router = APIRouter(
    prefix="/patients",
    tags=["Patients"]
)

@router.post("/", response_model=schemas.PatientResponse)
def create_patient(patient: schemas.PatientCreate, db: Session = Depends(get_db)):
    """
    Vytvoří nového pacienta. Nejdřív zkontroluje, jestli kód už neexistuje.
    """
    db_patient = services.get_patient_by_code(db, patient_code=patient.patient_code)
    
    if db_patient:
        # Pokud funkce něco našla, zastavíme to a hodíme Reactu chybu
        raise HTTPException(
            status_code=400, 
            detail=f"Pacient s kódem '{patient.patient_code}' už v databázi existuje."
        )
    
    return services.create_patient(db=db, patient=patient)


@router.get("/", response_model=List[schemas.PatientResponse])
def read_patients(
    skip: int = 0, 
    limit: int = 100, 
    search: Optional[str] = Query(None, description="Hledání v kódech pacientů (našeptávač)"),
    db: Session = Depends(get_db)
):
    """
    Vrátí seznam pacientů. Pokud je vyplněn parametr 'search', funguje jako našeptávač.
    """
    patients = services.get_patients(db, skip=skip, limit=limit, search=search)
    return patients