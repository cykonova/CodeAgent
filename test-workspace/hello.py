```python
#!/usr/bin/env python3
"""
A simple Python application.
"""

def greet(name):
    """
    Prints a personalized greeting message.
    
    Args:
        name (str): The person's name to be included in the greeting.
    """
    print(f"Hello, {name}!")

def farewell(name):
    """
    Prints a personalized goodbye message.
    
    Args:
        name (str): The person's name to be included in the farewell.
    """
    print(f"Goodbye, {name}!")

def get_current_time():
    """
    Returns the current time as a formatted string.
    """
    return datetime.now().strftime("%H:%M:%S")

import datetime

def main():
    """
    The main entry point of the application.
    Prints 'Hello, World!' by calling greet with 'World'.
    Prints the current time by calling get_current_time().
    Prints a personalized goodbye message by calling farewell with 'Alice'.
    """
    greet("World")
    print(get_current_time())
    farewell("Alice")

if __name__ == "__main__":
    main()
```