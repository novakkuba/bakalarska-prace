from pydantic import BaseModel, Field
from datetime import datetime

class PatientBase(BaseModel):
    # Field zajišťuje, že kód musí mít alespoň 1 znak (nesmí být prázdný)
    patient_code: str = Field(..., min_length=1, description="Anonymní kód pacienta, např. PAC-001")

class PatientCreate(PatientBase):
    pass

class PatientResponse(PatientBase):
    id: int
    created_at: datetime

    class Config:
        from_attributes = True