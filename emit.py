try:
    import c_formatter_42.run
    is_format = True
except:
    print("Please use `pip install c-formatter-42` for formatted output.")
    is_format = False

# Emitter object keeps track of the generated code and outputs it.
class Emitter:
    def __init__(self, fullPath):
        self.fullPath = fullPath
        self.header = ""
        self.code = ""

    def emit(self, code):
        self.code += code

    def emitLine(self, code):
        self.code += code + '\n'

    def headerLine(self, code):
        self.header += code + '\n'

    def writeFile(self):
        with open(self.fullPath, 'w') as outputFile:
            outputFile.write(c_formatter_42.run.run_all(self.header + self.code) if is_format else self.header + self.code)