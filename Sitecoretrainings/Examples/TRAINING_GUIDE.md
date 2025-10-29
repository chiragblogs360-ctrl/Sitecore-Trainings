# Sitecore Development Patterns Training Guide

## Overview: Processors, Pipelines, Events, and Jobs

This guide demonstrates the four fundamental patterns for extending Sitecore functionality.

---

## Quick Comparison Table

| Pattern | Purpose | Execution | When to Use |
|---------|---------|-----------|-------------|
| **Processor** | Single step in existing workflow | Synchronous, in sequence | Intercept specific point in Sitecore process |
| **Pipeline** | Complete custom workflow | Synchronous, ordered steps | Multi-step business logic you define |
| **Event** | React to something that happened | Synchronous, after action | Trigger secondary workflows |
| **Job** | Long-running background task | Asynchronous, separate thread | Time-consuming operations |

---

## 1. PROCESSOR

### What is it?
A **single step** that plugs into an **existing Sitecore pipeline**.

### When to use:
- Intercept a specific point in a Sitecore process
- Add custom validation or modification logic
- Execute code at a precise moment in the request lifecycle

### Example Scenarios:
- Check user permissions during request processing
- Modify item data before it's saved
- Redirect users based on custom rules
- Add custom headers to HTTP responses

### Key Characteristics:
- ✓ Plugs into **existing** pipelines
- ✓ Runs **synchronously** in order with other processors
- ✓ Can **abort** the pipeline
- ✓ Position controlled by config (patch:before/after)

### Code Location:
`Examples/1_ProcessorExample.cs`

---

## 2. PIPELINE

### What is it?
A **complete custom workflow** made up of multiple processors working together.

### When to use:
- Complex business logic requiring multiple steps
- Need to pass data between steps
- Want to create reusable, testable workflows

### Example Scenarios:
- Order processing workflow (validate → process → finalize)
- Content approval process (check permissions → validate content → approve)
- Data import workflow (parse → validate → transform → save)
- Multi-step calculations or transformations

### Key Characteristics:
- ✓ You define the **entire workflow**
- ✓ Multiple processors share data via args object
- ✓ Called explicitly from code: `CorePipeline.Run()`
- ✓ Each step can modify shared state
- ✓ Any processor can abort the pipeline

### Code Location:
`Examples/2_PipelineExample.cs`

---

## 3. EVENT HANDLER

### What is it?
Code that **reacts** to something that has happened in Sitecore.

### When to use:
- Trigger secondary actions after Sitecore operations
- Loose coupling between modules
- Audit logging
- Cache invalidation
- Integration with external systems

### Example Scenarios:
- Send email when content is published
- Clear cache when item is saved
- Audit log when item is deleted
- Notify external system when user logs in
- Update search index when item changes

### Key Characteristics:
- ✓ **Reactive** - responds to actions
- ✓ Multiple handlers can subscribe to same event
- ✓ Fires **after** action completes (item:saved) or **before** (item:saving)
- ✓ Great for loose coupling and separation of concerns
- ✓ Can create custom events with `Event.RaiseEvent()`

### Common Sitecore Events:
- `item:saved`, `item:saving`
- `item:deleted`, `item:deleting`
- `item:added`, `item:adding`
- `publish:end`, `publish:itemProcessed`
- `user:loggedin`, `user:loggedout`

### Code Location:
`Examples/3_EventHandlerExample.cs`

---

## 4. JOB

### What is it?
A **long-running or background task** that executes asynchronously in a separate thread.

### When to use:
- Operations that take more than a few seconds
- Don't want to block the user interface
- Need to report progress to users
- Scheduled or on-demand background processing

### Example Scenarios:
- Large data imports/exports
- Index rebuilds
- Batch processing thousands of items
- Report generation
- Media library maintenance
- Scheduled cleanup tasks

### Key Characteristics:
- ✓ Runs **asynchronously** in background thread
- ✓ Doesn't block the user
- ✓ Can report **progress** in real-time
- ✓ Can be **monitored, cancelled, resumed**
- ✓ Started on-demand (`JobManager.Start()`) or scheduled (agents)
- ✓ Visible in Sitecore Control Panel → Jobs

### Code Location:
`Examples/4_JobExample.cs`

---

## Decision Flow Chart

```
Need to extend Sitecore?
│
├─ Want to intercept existing Sitecore process?
│  └─ Use PROCESSOR
│
├─ Need multi-step custom workflow?
│  └─ Use PIPELINE
│
├─ React to something that happened?
│  └─ Use EVENT HANDLER
│
└─ Long-running or background task?
   └─ Use JOB
```

---

## Real-World Example Combinations

### Scenario 1: Content Publishing Workflow
```
1. PROCESSOR in publish pipeline validates content
2. PIPELINE runs custom approval logic (multi-step)
3. EVENT HANDLER fires on publish:end
4. JOB clears external caches in background
```

### Scenario 2: Data Import
```
1. JOB reads large CSV file in background
2. PIPELINE processes each batch (validate → transform → save)
3. Each PROCESSOR handles one transformation step
4. EVENT HANDLER logs import completion
```

### Scenario 3: User Registration
```
1. PROCESSOR validates user input during httpRequestBegin
2. PIPELINE handles multi-step registration (create user → assign roles → send email)
3. EVENT HANDLER on user:created triggers external CRM update
4. JOB sends welcome email series in background
```

---

## Code Examples Summary

All examples follow the same pattern:
1. Log "Hello World from [TYPE]" to Sitecore logs
2. Include minimal required code
3. Include config snippet (where needed)
4. Add comments explaining key concepts

### Files Created:
- `1_ProcessorExample.cs` - Single step in existing pipeline
- `2_PipelineExample.cs` - Complete custom workflow with multiple steps
- `3_EventHandlerExample.cs` - React to Sitecore events
- `4_JobExample.cs` - Background processing with progress tracking

---

## Testing Your Examples

### Test Processor:
1. Build and deploy code
2. Browse any Sitecore page
3. Check Sitecore logs for "Hello World from Processor"

### Test Pipeline:
```csharp
var args = new CustomPipelineArgs { Message = "test" };
CorePipeline.Run("customDataProcessing", args);
```

### Test Event Handler:
1. Open Content Editor
2. Save any item
3. Check logs for "Hello World from Event Handler"

### Test Job:
```csharp
var handle = JobStarter.StartSimpleJob();
// Check status in Control Panel → Jobs
// Or visit /sitecore/admin/jobs.aspx
```

---

## Best Practices

### Processors & Pipelines:
- Keep processors focused (single responsibility)
- Use meaningful arg class names
- Always check for null arguments
- Log important decisions
- Consider performance impact (runs on every request)

### Event Handlers:
- Keep handlers lightweight and fast
- Avoid blocking operations in event handlers
- Use jobs for time-consuming event responses
- Be careful with item:saving events (can impact performance)

### Jobs:
- Always update job status for user feedback
- Handle exceptions gracefully
- Check for job cancellation (`Job.Current.Status.State`)
- Set appropriate AfterLife for job cleanup
- Use meaningful job names and categories

---

## Additional Resources

### Sitecore Documentation:
- Pipelines: https://doc.sitecore.com/xp/en/developers/latest/sitecore-experience-manager/pipelines.html
- Events: https://doc.sitecore.com/xp/en/developers/latest/sitecore-experience-manager/events.html
- Jobs: https://doc.sitecore.com/xp/en/developers/latest/sitecore-experience-manager/execute-a-job.html

### Monitoring Jobs:
- Sitecore Control Panel → Jobs
- Admin page: `/sitecore/admin/jobs.aspx`

### Common Pipelines:
- `httpRequestBegin` - Start of HTTP request
- `httpRequestEnd` - End of HTTP request
- `mvc.renderRendering` - MVC rendering
- `item:save` - Item save operation
- `publishItem` - Item publish operation

---

## Training Exercise Ideas

1. **Modify Processor**: Change the log message based on user role
2. **Extend Pipeline**: Add a 4th processor to your custom pipeline
3. **Multiple Events**: Subscribe to item:saved and item:deleted
4. **Job with Progress**: Create a job that processes 100 items with progress updates
5. **Combine Patterns**: Use an event handler to start a job when items are published

---

**Happy Coding! 🚀**
