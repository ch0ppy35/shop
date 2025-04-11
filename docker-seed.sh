#!/bin/bash

# This script seeds the database in the Docker environment
# Run this after docker-compose up has started all services and migrations have completed

echo "Seeding products database in Docker environment..."

# Run the seed script in the Docker environment
docker-compose exec -T postgres bash -c "PGPASSWORD=postgres psql -U postgres -d products -c \"
-- Check if the products table exists and has data
DO \\\$\\\$
BEGIN
    -- Check if there's already data
    IF (SELECT COUNT(*) FROM products) > 0 THEN
        RAISE NOTICE 'Products table already has data, skipping seed.';
    ELSE
        RAISE NOTICE 'Seeding products table with initial data...';
        
        -- Insert sample products with inventory information
        INSERT INTO products (
            product_id, name, description, price, quantity, 
            sku, location, quantity_in_stock, reorder_threshold,
            created_at, updated_at
        ) VALUES 
        (
            'prod-001', 'Ergonomic Keyboard', 'Comfortable keyboard for long typing sessions', 
            89.99, 100, 'KB-ERG-001', 'Warehouse A', 50, 10,
            NOW(), NOW()
        ),
        (
            'prod-002', 'Wireless Mouse', 'High-precision wireless mouse', 
            49.99, 150, 'MS-WRL-002', 'Warehouse A', 75, 15,
            NOW(), NOW()
        ),
        (
            'prod-003', 'Ultra-wide Monitor', '34-inch curved ultra-wide monitor', 
            399.99, 30, 'MN-UW-003', 'Warehouse B', 15, 5,
            NOW(), NOW()
        ),
        (
            'prod-004', 'Mechanical Keyboard', 'Tactile mechanical keyboard with RGB lighting', 
            129.99, 80, 'KB-MCH-004', 'Warehouse A', 40, 8,
            NOW(), NOW()
        ),
        (
            'prod-005', 'Laptop Stand', 'Adjustable aluminum laptop stand', 
            39.99, 200, 'ACC-STD-005', 'Warehouse C', 100, 20,
            NOW(), NOW()
        ),
        (
            'prod-006', 'USB-C Hub', '7-in-1 USB-C hub with HDMI and card readers', 
            59.99, 120, 'ACC-HUB-006', 'Warehouse C', 60, 12,
            NOW(), NOW()
        ),
        (
            'prod-007', 'Wireless Headphones', 'Noise-cancelling wireless headphones', 
            199.99, 50, 'AUD-WH-007', 'Warehouse B', 25, 5,
            NOW(), NOW()
        ),
        (
            'prod-008', 'Webcam', '4K webcam with microphone', 
            79.99, 90, 'CAM-WEB-008', 'Warehouse B', 45, 10,
            NOW(), NOW()
        ),
        (
            'prod-009', 'External SSD', '1TB portable SSD drive', 
            149.99, 60, 'STR-SSD-009', 'Warehouse D', 30, 6,
            NOW(), NOW()
        ),
        (
            'prod-010', 'Gaming Mouse', 'High-DPI gaming mouse with programmable buttons', 
            69.99, 70, 'MS-GAM-010', 'Warehouse A', 35, 7,
            NOW(), NOW()
        );
        
        RAISE NOTICE 'Seed data inserted successfully.';
    END IF;
END
\\\$\\\$;
\""

echo "Docker seeding completed."
