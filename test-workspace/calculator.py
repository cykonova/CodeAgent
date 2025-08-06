import math

class Calculator:
    def add(self, num1, num2):
        return num1 + num2

    def subtract(self, num1, num2):
        return num1 - num2

    def multiply(self, num1, num2):
        return num1 * num2

    def divide(self, num1, num2):
        if num2 == 0:
            raise ZeroDivisionError("Cannot divide by zero.")
        return num1 / num2

    def power(self, base, exponent):
        return math.pow(base, exponent)

    def square_root(self, num):
        if num < 0:
            raise ValueError("Cannot calculate square root of negative number.")
        return math.sqrt(num)