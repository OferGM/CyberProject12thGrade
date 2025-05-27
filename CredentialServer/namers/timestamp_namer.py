"""timestamp_namer.py"""

import datetime
from interfaces.document_namer import IDocumentNamer

class TimestampDocumentNamer(IDocumentNamer):
    """Implementation of IDocumentNamer using application name and timestamp"""
    
    def generate_document_name(self, app_name: str, window_title: str) -> str:
        """Generate a document name based on app name and current timestamp"""
        # Sanitize app_name for file system compatibility
        app_name = ''.join(c if c.isalnum() or c in ['-', '_'] else '_' for c in app_name)
        timestamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        return f"{app_name}_{timestamp}"