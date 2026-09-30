using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class GoogleCalendarServiceTests
{
    [TestMethod]
    public void DesktopCredentialsAreParsed()
    {
        var credentials = GoogleCalendarService.ParseCredentials(
            """
            {
              "installed": {
                "client_id": "client.apps.googleusercontent.com",
                "client_secret": "secret",
                "token_uri": "https://oauth2.googleapis.com/token"
              }
            }
            """);

        Assert.AreEqual("client.apps.googleusercontent.com", credentials.ClientId);
        Assert.AreEqual("https://oauth2.googleapis.com/token", credentials.TokenUri);
    }

    [TestMethod]
    public void TodayEventsAreParsedInOrder()
    {
        var events = GoogleCalendarService.ParseEvents(
            """
            {
              "items": [
                {
                  "id": "one",
                  "summary": "Design review",
                  "location": "Google Meet",
                  "start": { "dateTime": "2026-09-29T09:30:00-04:00" },
                  "end": { "dateTime": "2026-09-29T10:15:00-04:00" }
                }
              ]
            }
            """);

        Assert.HasCount(1, events);
        Assert.AreEqual("Design review", events[0].Title);
        Assert.AreEqual("Google Meet", events[0].Location);
        Assert.IsFalse(events[0].IsAllDay);
        Assert.IsFalse(events[0].IsTask);
    }

    [TestMethod]
    public void DatedIncompleteTasksAreParsed()
    {
        var tasks = GoogleCalendarService.ParseTasks(
            """
            {
              "items": [
                {
                  "id": "task-one",
                  "title": "Submit expenses",
                  "status": "needsAction",
                  "due": "2026-09-29T00:00:00.000Z",
                  "webViewLink": "https://tasks.google.com/task-one"
                },
                {
                  "id": "task-two",
                  "title": "Already done",
                  "status": "completed",
                  "due": "2026-09-29T00:00:00.000Z"
                }
              ]
            }
            """,
            new GoogleCalendarService.GoogleTaskList("personal", "My Tasks"),
            new DateOnly(2026, 9, 29));

        Assert.HasCount(1, tasks);
        Assert.AreEqual("Submit expenses", tasks[0].Title);
        Assert.AreEqual("My Tasks", tasks[0].Location);
        Assert.IsTrue(tasks[0].IsAllDay);
        Assert.IsTrue(tasks[0].IsTask);
    }
}
