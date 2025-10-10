#!/usr/bin/env python3
import hashlib
import secrets

def hash_password(password):
    # Generate a random 16-byte salt and convert to hex
    salt_bytes = secrets.token_bytes(16)
    salt = salt_bytes.hex().upper()

    # Create password + salt combination
    password_salt = password + salt

    # Hash with SHA256
    hash_bytes = hashlib.sha256(password_salt.encode('utf-8')).digest()
    hash_hex = hash_bytes.hex().upper()

    return f"{salt}:{hash_hex}"

# Hash the test password
password = "Qq121212"
hashed = hash_password(password)
print(f"Password: {password}")
print(f"Hash: {hashed}")