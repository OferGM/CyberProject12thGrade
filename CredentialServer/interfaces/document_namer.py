"""document_namer.py"""

from abc import ABC, abstractmethod

class IDocumentNamer(ABC):
    """Interface for naming credential documents"""
    
    @abstractmethod
    def generate_document_name(self, app_name: str, window_title: str) -> str:
        """Generate a name for the credential document"""
        pass