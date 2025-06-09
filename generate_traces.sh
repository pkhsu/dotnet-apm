#!/bin/bash

echo "🚀 Starting Complex Trace Generation for Tempo Demo"
echo "=================================================="

API_BASE="http://localhost:8088"

# Function to generate a random order
generate_order() {
    local order_id=$1
    local customer_id="customer-$(( RANDOM % 100 + 1 ))"
    local product_id="product-$(( RANDOM % 20 + 1 ))"
    local quantity=$(( RANDOM % 5 + 1 ))
    local amount=$(( RANDOM % 1000 + 50 ))

    echo "📦 Creating Order $order_id..."
    echo "   Customer: $customer_id"
    echo "   Product: $product_id"  
    echo "   Quantity: $quantity"
    echo "   Amount: \$$amount"

    curl -s -X POST "$API_BASE/api/Order/create" \
         -H "Content-Type: application/json" \
         -d "{
           \"orderId\": \"$order_id\",
           \"customerId\": \"$customer_id\",
           \"productId\": \"$product_id\",
           \"quantity\": $quantity,
           \"amount\": $amount
         }" | jq '.'

    echo "   ✅ Order $order_id completed"
    echo ""
}

# Function to generate simple weather requests (for comparison)
generate_weather_request() {
    echo "🌤️  Fetching Weather Forecast..."
    curl -s "$API_BASE/weatherforecast" | jq '. | length'
    echo "   ✅ Weather request completed"
    echo ""
}

echo "Phase 1: Generating 5 Order API Calls (Complex Traces)"
echo "======================================================"

for i in {1..5}; do
    generate_order "order-$(date +%s)-$i"
    sleep 2
done

echo ""
echo "Phase 2: Generating 3 Weather API Calls (Simple Traces)"
echo "======================================================="

for i in {1..3}; do
    generate_weather_request
    sleep 1
done

echo ""
echo "Phase 3: Concurrent Orders (Parallel Traces)"
echo "============================================="

# Generate some concurrent orders
for i in {1..3}; do
    generate_order "concurrent-order-$i" &
done

# Wait for all background jobs to complete
wait

echo ""
echo "🎉 Trace generation completed!"
echo "==============================================="
echo "📊 Check your traces in:"
echo "   • Grafana: http://localhost:3000"
echo "   • Tempo directly: http://localhost:3200"
echo "   • Prometheus metrics: http://localhost:9090"
echo ""
echo "🔍 Look for traces with these patterns:"
echo "   • Complex traces: Order API calls with 4-5 spans"
echo "   • Simple traces: Weather API calls with 1-2 spans"
echo "   • Parallel traces: Concurrent execution patterns" 