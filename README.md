# BlackScholesTaskTimeEstimator Effort Estimation using Black-Scholes Model

## Overview

This project implements an innovative adaptation of the Black-Scholes financial model for effort estimation in project management. By treating task estimates as financial options, it provides a risk-aware approach to project planning that accounts for uncertainty and changing conditions.

The application allows you to:
- Estimate task completion probabilities
- Calculate volatility based on historical data
- Perform comparative analysis of multiple tasks
- Identify high-risk tasks and allocate resources effectively

![Example Output](https://via.placeholder.com/800x400?text=Screenshot+of+Estimation+Results)

## Key Features

- **Financial Modeling for Project Management**: Adapts Black-Scholes options pricing to effort estimation
- **Volatility Calculation**: Measures estimation uncertainty using historical data
- **Multi-Task Analysis**: Compare and prioritize multiple tasks simultaneously
- **Risk Assessment**: Identifies tasks with high risk of exceeding targets
- **Actionable Insights**: Provides concrete recommendations for risk mitigation

## Installation

1. Ensure you have [.NET 6.0 SDK](https://dotnet.microsoft.com/download) installed
2. Clone the repository:
```bash
git clone https://github.com/pwrmind/effort-estimation-black-scholes.git
```
3. Navigate to the project directory:
```bash
cd effort-estimation-black-scholes
```
4. Run the application:
```bash
dotnet run
```

## Usage

### Input Parameters
The model uses five key parameters for estimation:

| Parameter | Symbol | Description | Example |
|-----------|--------|-------------|---------|
| Current Estimate | S | Initial time estimate for the task | 40 hours |
| Target Effort | K | Maximum acceptable effort | 50 hours |
| Time to Deadline | T | Time until deadline (in years) | 0.5 (6 months) |
| Volatility | σ | Uncertainty in estimates | 0.25 (25%) |
| Risk-Free Rate | r | Expected productivity growth | 0.05 (5%) |

### Workflow
1. **Select volatility mode**: Choose between common volatility for all tasks or individual volatility per task
2. **Calculate volatility**: Use historical data or enter a custom value
3. **Enter task parameters**: Provide estimates and constraints for each task
4. **Analyze results**: Review the summary table and risk analysis
5. **Implement recommendations**: Use insights to optimize your project plan

## Mathematical Foundation

The adapted Black-Scholes formula for effort estimation:

$$ \text{Effort} = S \cdot N(d_1) - K \cdot e^{-rT} \cdot N(d_2) $$

Where:
$$ d_1 = \frac{\ln(S/K) + (r + \sigma^2/2) T}{\sigma \sqrt{T}} $$
$$ d_2 = d_1 - \sigma \sqrt{T} $$

- $N(x)$: Cumulative distribution function of the standard normal distribution
- $S$: Current effort estimate
- $K$: Target effort
- $T$: Time to deadline
- $\sigma$: Volatility of effort estimates
- $r$: Risk-free productivity growth rate

## Example Scenario

```text
📊 SUMMARY RESULTS TABLE
====================================================================================================
| Task Name       | Current (h) | Target (h) | Time (yrs) | Volatility | Forecast (h) | Status   |
====================================================================================================
| API Development |       50.0  |       60.0 |      0.500 |      22.5% |       54.3   | ✅ Success|
| System Testing  |       40.0  |       35.0 |      0.300 |      22.5% |       42.8   | ⚠️ Risk   |
====================================================================================================

🔍 RISK ANALYSIS
• Total tasks: 2
• High-risk tasks: 1 (50.0%)
• High-success-probability tasks: 1

🚨 HIGH-RISK TASKS:
- System Testing: Overshoot by 7.8h (22.3%)

💡 RECOMMENDATIONS FOR RISKY TASKS:
1. Review work scope (Scope Reduction)
2. Add additional resources
3. Conduct estimation review with experts
4. Consider deadline adjustment
```

## Customization Options

The application is designed for extensibility:

1. **Volatility Calculation Strategies**:
   - Implement `IVolatilityCalculator` interface
   - Add new calculation methods (e.g., team-specific, task-type specific)

2. **Normal Distribution Implementations**:
   - Implement `INormalDistribution` interface
   - Add alternative approximation methods

3. **Reporting Formats**:
   - Extend the reporting module to support CSV, Excel, or PDF outputs
   - Add graphical visualizations of results

## Limitations

While innovative, this approach has certain limitations:
- Best suited for tasks with moderate to high uncertainty
- Requires historical data for accurate volatility calculation
- Assumes normal distribution of estimation errors
- Time units must be consistent (years for deadlines)

## Contributing

Contributions are welcome! Please follow these steps:
1. Fork the repository
2. Create a new branch (`git checkout -b feature/your-feature`)
3. Commit your changes (`git commit -am 'Add some feature'`)
4. Push to the branch (`git push origin feature/your-feature`)
5. Create a new Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Fischer Black and Myron Scholes for their original options pricing model
- Researchers who pioneered financial mathematics applications in project management
- The .NET community for excellent development tools