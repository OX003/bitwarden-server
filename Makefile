SHELL := /bin/bash

.PHONY: all
all: lint test build

.PHONY: lint
lint:
	dotnet format --verify-no-changes --verbosity diagnostic

.PHONY: test
test:
	echo "Testing"

.PHONY: build
build:
	echo "Building"
	AZ_REGISTRY=bitwarden.prod.azurecr.io
	IMAGE_TAG=dev-$${GITHUB_SHA:0:8}
	echo "### :mega: Docker Image Tag: $$IMAGE_TAG" >> $$GITHUB_STEP_SUMMARY
	FULL_IMAGE_NAME=$${AZ_REGISTRY}/$${PROJECT_NAME}:$${IMAGE_TAG}
	CACHE_IMAGE_NAME=$${AZ_REGISTRY}/$${PROJECT_NAME}:buildcache
	docker build --help
	docker build --cache-from "type=registry,ref=$$CACHE_IMAGE_NAME" \
	  --cache-to "type=registry,ref=$$CACHE_IMAGE_NAME,mode=max" \
	  --file $$DOCKER_FILE \
	  --platform "linux/amd64,linux/arm/v7,linux/arm64" \
	  --push \
	  --tag $$FULL_IMAGE_NAME \
	  .

.PHONY: clean
clean:
	echo "Cleaning"
