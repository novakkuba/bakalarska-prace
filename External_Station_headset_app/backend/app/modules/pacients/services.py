from sqlalchemy.orm import Session
from app.models import Patient 
from . import schemas

def get_patient_by_code(db: Session, patient_code: str):
    """
    Pomocná funkce: Najde pacienta podle přesného kódu.
    Použijeme ji při vytváření, abychom zabránili duplicitám.
    """
    return db.query(Patient).filter(Patient.patient_code == patient_code).first()

def create_patient(db: Session, patient: schemas.PatientCreate):
    """
    Zapíše nového pacienta do databáze.
    """
    db_patient = Patient(patient_code=patient.patient_code)
    db.add(db_patient)
    db.commit()
    db.refresh(db_patient) 
    return db_patient

def get_patients(db: Session, skip: int = 0, limit: int = 100, search: str = None):
    """
    Našeptávač a výpis pacientů.
    Pokud přijde parametr 'search', databáze vyfiltruje jen částečné shody.
    """
    query = db.query(Patient)
    
    # našeptavač
    if search:
        query = query.filter(Patient.patient_code.ilike(f"%{search}%"))
        
    return query.offset(skip).limit(limit).all()