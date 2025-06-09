#!/bin/bash

echo "🚀 Testing optimized Grafana dashboard..."
echo "📊 Generating diverse API traffic..."

# Function to make WeatherForecast calls
weather_calls() {
    echo "🌤️  Making WeatherForecast API calls..."
    for i in {1..10}; do
        curl -s "http://localhost:8088/WeatherForecast" > /dev/null
        sleep 0.5
    done
}

# Function to simulate some errors (call non-existent endpoints)
error_calls() {
    echo "❌ Generating some 404 errors..."
    for i in {1..3}; do
        curl -s "http://localhost:8088/NonExistent" > /dev/null
        sleep 1
    done
}

# Function to check metrics endpoint
metrics_calls() {
    echo "📈 Accessing metrics endpoint..."
    for i in {1..5}; do
        curl -s "http://localhost:8088/metrics" > /dev/null
        sleep 0.3
    done
}

# Run all test functions in background for parallel execution
weather_calls &
error_calls &
metrics_calls &

# Wait for all background jobs to complete
wait

echo "✅ Test traffic generation completed!"
echo ""
echo "🎯 Dashboard should now show:"
echo "   • Request rate metrics for WeatherForecast, /metrics endpoints"
echo "   • HTTP status code distribution (200s and 404s)"
echo "   • Request latency percentiles"
echo "   • Request duration by method (GET)"
echo "   • Top slowest endpoints table"
echo "   • Application logs with trace IDs"
echo ""
echo "🌐 Access your dashboard at: http://localhost:3000"
echo "📱 Login: admin / admin"
echo "📊 Navigate to: Sample.Api Observability Dashboard" 