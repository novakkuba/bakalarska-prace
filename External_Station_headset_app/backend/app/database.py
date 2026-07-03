"""
Základní konfigurace připojení k PostgreSQL databázi pomocí SQLAlchemy.
Obsahuje definici enginu a poskytuje správce relací (pro API i procesy na pozadí) k bezpečnému přístupu k datům.
"""

import os
from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker, declarative_base
from contextlib import contextmanager


DB_USER = os.getenv("DB_USER")
DB_PASSWORD = os.getenv("DB_PASSWORD")
DB_HOST = os.getenv("DB_HOST", "db")
DB_NAME = os.getenv("DB_NAME")

DATABASE_URL = f"postgresql://{DB_USER}:{DB_PASSWORD}@{DB_HOST}/{DB_NAME}"

engine = create_engine(DATABASE_URL)

SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

Base = declarative_base()

def get_db():
    """
    FastAPI calls this for every request. 
    It creates a session, lets the route use it, and closes it.
    """
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

@contextmanager
def session_scope():
    """
    Otevře session, pohlídá chyby a VŽDY ji po sobě zavře.
    Commit neprovádí automaticky - to je na volajícím.
    """
    db = SessionLocal()
    try:
        yield db
    except Exception:
        db.rollback() # Pokud nastane chyba, vrátíme změny zpět
        raise         # A chybu pošleme dál, aby o ní listener věděl
    finally:
        db.close()