"""connection_handler.py"""

from abc import ABC, abstractmethod

class IConnectionHandler(ABC):
    """Interface for handling client connections"""
    
    @abstractmethod
    def start(self):
        """Start listening for connections"""
        pass
    
    @abstractmethod
    def stop(self):
        """Stop listening for connections"""
        pass
    
    @abstractmethod
    def handle_client(self, client_socket, client_address):
        """Handle an individual client connection"""
        pass