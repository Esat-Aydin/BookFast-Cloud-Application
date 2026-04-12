// ******************************************************************************
//  © 2026 Ernst & Young Accountants LLP - www.ey.com
//
//  Author          : EY - Climate Change and Sustainability Services
//  File:           : MessageProcessingOutcome.cs
//  Project         : BookFast.Reporting.Functions
// ******************************************************************************

namespace BookFast.Reporting.Functions.Processing;

public enum MessageProcessingOutcome
{
    Processed,

    AlreadyProcessed,

    Skipped,

    DeadLettered
}
