"""credential_model.py"""

from typing import Dict, List
import datetime

class CredentialModel:
    """Model representing captured credentials"""
    
    def __init__(self, app_name: str, window_title: str, process_name: str, url: str, timestamp: datetime.datetime,
                 form_fields: List[Dict], keystrokes: List[Dict]):
        self.app_name = app_name
        self.window_title = window_title
        self.process_name = process_name
        self.url = url
        self.timestamp = timestamp
        self.form_fields = form_fields
        self.keystrokes = keystrokes
        
    def to_dict(self) -> Dict:
        """Convert the model to a dictionary for storage"""
        return {
            "app_name": self.app_name,
            "window_title": self.window_title,
            "process_name": self.process_name,
            "url": self.url,
            "timestamp": self.timestamp,
            "form_fields": self.form_fields,
            "keystrokes": self.keystrokes
        }