.PHONY all
all: lint test build

.PHONY lint
lint:
	dotnet format --verify-no-changes

.PHONY test
test:
	echo "Testing"

.PHONY build
build:
	echo "Building"

.PHONY clean
clean:
	echo "Cleaning"
