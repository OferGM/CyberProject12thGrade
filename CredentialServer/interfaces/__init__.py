"""__init__.py"""

"""
Interfaces defining the core abstractions for the Credential Server.
"""

from .credential_repository import ICredentialRepository
from .connection_handler import IConnectionHandler
from .document_namer import IDocumentNamer
from .credential_processor import ICredentialProcessor

__all__ = [
    'ICredentialRepository',
    'IConnectionHandler', 
    'IDocumentNamer',
    'ICredentialProcessor'
]