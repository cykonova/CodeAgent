#!/usr/bin/env python3

import datetime

def greet(name):
    print(f"Hello, {name}!")

def farewell(name):
    print(f"Goodbye, {name}!")

def get_current_time():
    return datetime.datetime.now().strftime("%H:%M:%S")

def main():
    greet("World")
    print(get_current_time())
    farewell("Alice")

if __name__ == "__main__":
    main()