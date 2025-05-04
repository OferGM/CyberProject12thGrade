from abc import ABC, abstractmethod
from typing import Dict, List

class ICredentialRepository(ABC):
    """Interface for credential storage implementations"""
    
    @abstractmethod
    def save_credentials(self, application_info: Dict, form_fields: List[Dict], keystrokes: List[Dict]) -> str:
        """Save captured credentials and return document ID"""
        pass
    
    @abstractmethod
    def get_credentials_by_app(self, app_name: str) -> List[Dict]:
        """Retrieve credentials by application name"""
        pass
    
    @abstractmethod
    def get_all_credentials(self) -> List[Dict]:
        """Retrieve all stored credentials"""
        pass