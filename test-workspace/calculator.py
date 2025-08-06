```python
import math

class Calculator:
    def add(self, num1, num2):
        """Returns the sum of two numbers."""
        return num1 + num2

    def subtract(self, num1, num2):
        """Returns the difference of two numbers."""
        return num1 - num2

    def multiply(self, num1, num2):
        """Returns the product of two numbers."""
        return num1 * num2

    def divide(self, num1, num2):
        """Returns the quotient of two numbers. Raises ZeroDivisionError if denominator is zero."""
        if num2 == 0:
            raise ZeroDivisionError("Cannot divide by zero.")
        return num1 / num2

    def power(self, base, exponent):
        """Raises the first number to the power of the second."""
        return math.pow(base, exponent)

    def square_root(self, num):
        """Returns the square root of a number."""
        if num < 0:
            raise ValueError("Cannot calculate square root of negative number.")
        return math.sqrt(num)
```