SHELL := /usr/bin/env bash -o pipefail
.SHELLFLAGS := -ec

AZ_REGISTRY := bitwardenprod.azurecr.io
IMAGE_TAG := dev-$${GITHUB_SHA:0:8}
FULL_IMAGE_NAME := $(AZ_REGISTRY)/$${PROJECT_NAME}:$(IMAGE_TAG)
CACHE_IMAGE_NAME := $(AZ_REGISTRY)/$${PROJECT_NAME}:buildcache

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
	@echo "Building"
	@echo "### :mega: Docker Image Tag: $$IMAGE_TAG" >> $$GITHUB_STEP_SUMMARY
	docker build --cache-from "type=registry,ref=$(CACHE_IMAGE_NAME)" \
	  --cache-to "type=registry,ref=$(CACHE_IMAGE_NAME),mode=max" \
	  --file "$$DOCKER_FILE" \
	  --platform "linux/amd64,linux/arm/v7,linux/arm64" \
	  --push \
	  --tag $(FULL_IMAGE_NAME) \
	  .

.PHONY: clean
clean:
	echo "Cleaning"
