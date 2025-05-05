from abc import ABC, abstractmethod
from typing import Dict

class ICredentialProcessor(ABC):
    """Interface for processing received credential data"""
    
    @abstractmethod
    def process_credentials(self, data: Dict, client_ip: str = None) -> Dict:
        """Process the received credential data with optional client IP"""
        pass