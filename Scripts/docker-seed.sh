#!/bin/bash

# This script seeds the database in the Docker environment
# Run this after docker-compose up has started all services and migrations have completed

echo "Seeding products database in Docker environment..."

# The seeding is now handled by the ProductSeeder class in the Products service
# This script is kept for backward compatibility but doesn't need to do anything

echo "Seeding is now handled automatically by the Products service on startup."

echo "Docker seeding completed."
