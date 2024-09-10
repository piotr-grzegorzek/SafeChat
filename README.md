# P2P Chat with RSA and AES Encryption

## Overview

This is a simple P2P (peer-to-peer) chat application implemented in C#. The application ensures secure communication between two parties by utilizing RSA and AES encryption, hashing for message integrity, and digital signatures for authentication. The application is built using the `.NET` framework and the `System.Security.Cryptography` library.

## Features

- **RSA Encryption**: Asymmetric encryption is used for initial key exchange and authentication between peers.
- **AES Encryption**: Symmetric encryption is used for fast and secure message encryption during the chat session.
- **SHA-256 Hashing**: Ensures message integrity by generating and verifying hashes of messages.
- **RSA Digital Signatures**: Used to authenticate the sender of the message.

## Cryptographic Approach

### Integrity
- The sender generates a SHA-256 hash of the message and sends it along with the encrypted message.
- The recipient verifies the message integrity by calculating the SHA-256 hash of the decrypted message and comparing it to the received hash.

### RSA Encryption
- The sender encrypts the session key with the recipient's public key.
- The recipient decrypts the message using their private key. RSA is used to establish trust and exchange session keys securely.

### AES Encryption
- After establishing the session, AES encryption is used for fast and efficient encryption and decryption of messages.

### RSA Digital Signatures
- The sender signs the message with their private RSA key.
- The recipient verifies the signature with the sender’s public RSA key, ensuring the message's authenticity.

## Security Model

We implement a **sign-then-encrypt** model where:
1. The message is signed with the sender's private key.
2. The message is encrypted using the session's AES key.
3. The recipient decrypts the message, verifies the hash for integrity, and checks the signature for authenticity.

## How It Works

### Initialization
1. Both peers generate RSA key pairs (public and private keys).
2. They establish a connection via sockets, where one peer acts as the **server** and the other as the **client**.

### Key Exchange
1. The client initiates a session by sending a connection request along with its public RSA key.
2. The server responds with its own public RSA key.
3. The client generates a random AES key for the chat session.
4. The AES key is encrypted using the server's RSA public key.
5. A hash of the AES key is created using SHA-256.
6. The client signs the hash using its private RSA key and sends the encrypted AES key, hash, and signature to the server.
7. The server decrypts the AES key using its private RSA key, verifies the hash, and checks the signature using the client's public RSA key.

### Sending a Message
1. The sender encrypts the message using the session's AES key.
2. A SHA-256 hash of the plaintext message is created.
3. The sender signs the hash with their private RSA key.
4. The encrypted message, hash, and signature are sent to the recipient.

### Receiving a Message
1. The recipient decrypts the message using the AES session key.
2. The recipient verifies the hash by calculating SHA-256 for the decrypted message and comparing it with the received hash.
3. The recipient verifies the signature using the sender’s public RSA key.
