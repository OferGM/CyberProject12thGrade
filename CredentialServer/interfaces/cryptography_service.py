"""cryptography_service.py"""

from abc import ABC, abstractmethod

class ICryptographyService(ABC):
    """Interface for cryptography operations"""
    
    @abstractmethod
    def encrypt(self, plain_text: str) -> str:
        """Encrypt plain text and return base64 encoded cipher text"""
        pass
    
    @abstractmethod
    def decrypt(self, cipher_text: str) -> str:
        """Decrypt base64 encoded cipher text and return plain text"""
        pass
    
    @abstractmethod
    def encrypt_bytes(self, plain_bytes: bytes) -> bytes:
        """Encrypt raw bytes"""
        pass
    
    @abstractmethod
    def decrypt_bytes(self, cipher_bytes: bytes) -> bytes:
        """Decrypt raw bytes"""
        pass