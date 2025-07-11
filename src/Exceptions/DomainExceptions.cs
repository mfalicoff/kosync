using System;

namespace Kosync.Exceptions;

public abstract class DomainException(string message) : Exception(message);

public class UserAlreadyExistsException(string username) : DomainException($"User '{username}' already exists");

public class RegistrationDisabledException() : DomainException("User registration is currently disabled");

public class UserNotFoundException(string username) : DomainException($"User '{username}' not found");
