"""aes_cryptography_service.py"""
import base64
from Crypto.Cipher import AES
from Crypto.Util.Padding import pad, unpad
import sys
import os
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from interfaces.cryptography_service import ICryptographyService

class AESCryptographyService(ICryptographyService):
    """AES encryption implementation using CBC mode"""
    
    def __init__(self, base64_key: str = None, base64_iv: str = None):
        """Initialize with base64 encoded key and IV"""
        # Default keys should match the C# implementation
        self.key = base64.b64decode(base64_key or "dGhpc2lzYXZlcnlzZWN1cmVrZXkxMjM0NTY3ODkwMTI=")
        self.iv = base64.b64decode(base64_iv or "dGhpc2lzMTZieXRlc2l2IQ==")
    
    def encrypt(self, plain_text: str) -> str:
        """Encrypt plain text and return base64 encoded cipher text"""
        if not plain_text:
            return ""
        
        plain_bytes = plain_text.encode('utf-8')
        encrypted_bytes = self.encrypt_bytes(plain_bytes)
        return base64.b64encode(encrypted_bytes).decode('utf-8')
    
    def decrypt(self, cipher_text: str) -> str:
        """Decrypt base64 encoded cipher text and return plain text"""
        if not cipher_text:
            return ""
        
        cipher_bytes = base64.b64decode(cipher_text)
        decrypted_bytes = self.decrypt_bytes(cipher_bytes)
        return decrypted_bytes.decode('utf-8')
    
    def encrypt_bytes(self, plain_bytes: bytes) -> bytes:
        """Encrypt raw bytes using AES-CBC with PKCS7 padding"""
        if not plain_bytes:
            return b""
        
        # Create cipher
        cipher = AES.new(self.key, AES.MODE_CBC, self.iv)
        
        # Pad data using PKCS7
        padded_data = pad(plain_bytes, AES.block_size)
        
        # Encrypt
        encrypted = cipher.encrypt(padded_data)
        
        return encrypted
    
    def decrypt_bytes(self, cipher_bytes: bytes) -> bytes:
        """Decrypt raw bytes using AES-CBC with PKCS7 padding"""
        if not cipher_bytes:
            return b""
        
        # Create cipher
        cipher = AES.new(self.key, AES.MODE_CBC, self.iv)
        
        # Decrypt
        decrypted_padded = cipher.decrypt(cipher_bytes)
        
        # Remove padding
        decrypted = unpad(decrypted_padded, AES.block_size)
        
        return decrypted