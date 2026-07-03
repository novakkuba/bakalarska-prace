"""
Definuje SQLAlchemy databázové modely pro Multi-Device a Multi-Patient architekturu.
Zajišťuje relační propojení mezi pacientem, VR headsetem, sezením a samotnými herními logy.
Zahrnuje kaskádové mazání a podporuje anonymizaci dat (Patient Code).
"""

from sqlalchemy import Column, Integer, String, DateTime, ForeignKey, JSON, Boolean
from sqlalchemy.orm import relationship
from datetime import datetime

from .database import Base

class Patient(Base):
    __tablename__ = "patients"

    id = Column(Integer, primary_key=True, index=True)
    
    patient_code = Column(String, unique=True, index=True, nullable=False)
    created_at = Column(DateTime, default=datetime.now)

    # RELACE: Pacient může mít historii mnoha sezení
    sessions = relationship("Session", back_populates="patient", cascade="all, delete-orphan")

class Device(Base):
    __tablename__ = "devices"

    # Normální číselné ID (jako u ostatních)
    id = Column(Integer, primary_key=True, index=True) 
    
    # Unikátní string z Unity:
    hardware_id = Column(String, unique=True, index=True, nullable=False) 
    
    name = Column(String, default="Neznámý Headset") 
    last_seen = Column(DateTime, default=datetime.now)
    is_online = Column(Boolean, default=False) 

    sessions = relationship("Session", back_populates="device", cascade="all, delete-orphan")

class Session(Base):
    __tablename__ = "sessions"

    id = Column(Integer, primary_key=True, index=True)
    
    
    device_id = Column(Integer, ForeignKey("devices.id"), nullable=False)
    patient_id = Column(Integer, ForeignKey("patients.id"), nullable=False)
    
    is_active = Column(Boolean, default=True) 
    start_time = Column(DateTime, default=datetime.now) 
    end_time = Column(DateTime, nullable=True)
    
    patient = relationship("Patient", back_populates="sessions")
    device = relationship("Device", back_populates="sessions")
    logs = relationship("Logs", back_populates="session", cascade="all, delete-orphan")

class Logs(Base):
    __tablename__ = "logs"

    id = Column(Integer, primary_key=True, index=True)
    session_id = Column(Integer, ForeignKey("sessions.id"), nullable=False)
    
    topic = Column(String, index=True) # Např. vrapp/a1b2c3d4/logs
    iteration = Column(Integer, default=0)

    timestamp = Column(DateTime, default=datetime.now)
    data = Column(JSON) # Surová data z MQTT

    
    session = relationship("Session", back_populates="logs")