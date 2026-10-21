using System.Net;
using System.Text;
using System.Text.Json;
using VMIdentityLab.VMware;
using VMIdentityLab.VMware.Configuration;

namespace VMIdentityLab.IntegrationTests;

public sealed partial class ApiConfigurationTests
{
    private const string IdentityMac = "00:50:56:00:00:01";
    private const string IdentityUuid = "56 4d 11 22 33 44 55 66-77 88 99 aa bb cc dd ee";
    private const string ExistingIdentity = "# identity synthetic only\nvirtualHW.version = \"19\"\nmemsize = \"6144\"\nnumvcpus = \"4\"\ncpuid.coresPerSocket = \"2\"\n"
        + "ethernet0.present = \"TRUE\"\nethernet0.addressType = \"static\"\nethernet0.connectionType = \"hostonly\"\nethernet0.startConnected = \"FALSE\"\n"
        + "ethernet0.address = \"" + IdentityMac + "\"\nuuid.bios = \"" + IdentityUuid + "\"\nunknown.setting = \"retain\"\n";
    private static Fixture IdentityFixture() => new(Encoding.UTF8.GetBytes(ExistingIdentity));
    private static Task<VmxPreparedChange> IdentityPreview(Fixture f, IdentityConfigField field = IdentityConfigField.StaticMac, Version? version = null)
        => VmxIdentityConfigPlanner.PrepareAsync(f.Path, version ?? Version, field, Token);

    [Theory]
    [InlineData("# comment \"original\"")]
    [InlineData("# \"\" # nested \"text\"")]
    [InlineData("# unmatched \" quote")]
    public async Task IdentityApiPreservesUnchangedQuotedInlineComments(string comment)
    {
        using var f = new Fixture(Encoding.UTF8.GetBytes(ExistingIdentity.Replace("unknown.setting = \"retain\"",
            "unknown.setting = \"retain\" " + comment, StringComparison.Ordinal)));
        var preview = await IdentityPreview(f);
        var receipt = await new VmxApiConfigManager(f.Api, () => Version, files: f.Files).ApplyAsync(preview, preview.Id, Token);
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(receipt.BackupPath, Token));
        Assert.Contains(comment, await File.ReadAllTextAsync(f.Path, Token));
        Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
        Assert.Equal(1, f.Server.PutCount);
        Assert.Equal(0, f.Files.ReplaceCount);
    }

    [Theory]
    [InlineData(IdentityConfigField.StaticMac)]
    [InlineData(IdentityConfigField.BiosUuid)]
    public async Task ReleasedIdentityStillRequiresCurrentConfirmationAndCannotUseLegacyFileBackend(IdentityConfigField field)
    {
        using var f = IdentityFixture();
        var preview = await IdentityPreview(f, field);
        Assert.True(VmxApiConfigManager.IsProductionWriteApproved(preview));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new VmxApiConfigManager(f.Api, () => Version).ApplyAsync(preview, Guid.Empty, Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new VmxConfigManager(writePolicy: new CandidatePolicy(), files: f.Files).ApplyAsync(preview, preview.Id, Token));
        Assert.Empty(f.Server.Requests);
        Assert.Single(Directory.GetFiles(f.Folder));
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(f.Path, Token));
    }

    [Theory]
    [InlineData("17.5.2")]
    [InlineData("17.5.2.23775572")]
    [InlineData("17.6.0.0")]
    public async Task IdentityIntrinsicBuildRestrictionCannotBeOverriddenByAnInjectedPolicy(string version)
    {
        using var f = IdentityFixture();
        var installed = System.Version.Parse(version);
        var preview = await IdentityPreview(f, version: installed);
        Assert.False(VmxApiConfigManager.IsProductionWriteApproved(preview));
        await Assert.ThrowsAsync<InvalidOperationException>(() => new VmxApiConfigManager(f.Api, () => installed).ApplyAsync(preview, preview.Id, Token));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Manager(() => installed).ApplyAsync(preview, preview.Id, Token));
        Assert.Empty(f.Server.Requests);
        Assert.Single(Directory.GetFiles(f.Folder));
    }

    [Theory]
    [InlineData(IdentityConfigField.StaticMac)]
    [InlineData(IdentityConfigField.BiosUuid)]
    public async Task IdentityApiApplyHistoryReopenAndConfirmedRestorePreserveLaterUnrelatedMetadata(IdentityConfigField field)
    {
        using var f = IdentityFixture();
        var historyPath = Path.Combine(f.Folder, "history");
        var manager = new VmxApiConfigManager(f.Api, () => Version, files: f.Files);
        var workflow = new VmxConfigWorkflow(manager, new LocalVmxBackupHistory(historyPath));
        var preview = await IdentityPreview(f, field);
        var applied = await workflow.ApplyAsync(preview, preview.Id, false, Token);
        var receipt = applied.Receipt;
        Assert.Null(receipt.BeforeSettings);
        Assert.Null(receipt.AfterSettings);
        Assert.NotNull(receipt.BeforeIdentity);
        Assert.NotNull(receipt.AfterIdentity);
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(receipt.BackupPath, Token));
        Assert.NotEqual(preview.Plan.ProposedHash, receipt.AppliedHash); // Observed CRLF/order normalization, not a byte-equality claim.
        Assert.Equal(VmxDocument.Parse(await File.ReadAllBytesAsync(f.Path, Token)).ContentHash, receipt.AppliedHash);
        Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
        using (var stored = JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(historyPath, applied.Id.ToString("N") + ".json"), Token)))
            Assert.Equal(3, stored.RootElement.GetProperty("schemaVersion").GetInt32());
        var reopened = new LocalVmxBackupHistory(historyPath);
        var read = await reopened.ReadAsync(Token);
        Assert.Equal(0, read.UnreadableFiles);
        Assert.Equal(applied, Assert.Single(read.Entries));
        await File.AppendAllTextAsync(f.Path, "# unrelated later metadata\r\ndisplayName = \"new unrelated name\"\r\n", Token);
        var latest = await File.ReadAllBytesAsync(f.Path, Token);
        var restore = await VmxApiConfigManager.PrepareRestoreAsync(f.Path, read.Entries[0].Receipt, Token);
        Assert.Equal(VmxDocument.Parse(latest).ContentHash, restore.Plan.OriginalHash);
        var requests = f.Server.Requests.Count;
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ApplyAsync(restore, preview.Id, Token));
        Assert.Equal(requests, f.Server.Requests.Count);
        await new VmxConfigWorkflow(new VmxApiConfigManager(f.Api, () => Version, files: f.Files), reopened)
            .ApplyAsync(restore, restore.Id, true, Token);
        Assert.Equal(2, (await reopened.ReadAsync(Token)).Entries.Count);
        var values = VmxDocument.Parse(await File.ReadAllBytesAsync(f.Path, Token)).Entries.ToDictionary(item => item.Key, item => item.Value);
        Assert.Equal(IdentityMac, values["ethernet0.address"]);
        Assert.Equal(IdentityUuid, values["uuid.bios"]);
        Assert.Equal("new unrelated name", values["displayName"]);
        Assert.Contains("# unrelated later metadata", await File.ReadAllTextAsync(f.Path, Token), StringComparison.Ordinal);
        Assert.Equal(2, f.Server.PutCount);
        Assert.All(f.Server.Requests.Where(request => request.StartsWith("PUT", StringComparison.Ordinal)),
            request => Assert.Equal("PUT /api/vms/test-vm/params", request));
        Assert.Equal(0, f.Files.ReplaceCount);
    }

    [Theory]
    [InlineData("backup")]
    [InlineData("corruptBackup")]
    [InlineData("stage")]
    [InlineData("corruptStage")]
    [InlineData("intent")]
    [InlineData("corruptIntent")]
    public async Task IdentityBackupStagingAndIntentFailuresCannotSendPut(string failure)
    {
        using var f = IdentityFixture();
        f.Files.Failure = failure;
        var preview = await IdentityPreview(f);
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.False(error.OutcomeUnknown);
        Assert.Equal(0, f.Server.PutCount);
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(f.Path, Token));
        Assert.Equal(0, f.Files.ReplaceCount);
    }

    [Theory]
    [InlineData("poweredOn")]
    [InlineData("suspended")]
    [InlineData("unrecognized")]
    public async Task IdentityWriteRequiresPoweredOffBeforeBackup(string state)
    {
        using var f = IdentityFixture();
        f.Server.Power = state;
        var preview = await IdentityPreview(f);
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.False(error.OutcomeUnknown);
        Assert.Equal(0, f.Server.PutCount);
        Assert.False(File.Exists(error.BackupPath));
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(f.Path, Token));
    }

    [Theory]
    [InlineData("mac")]
    [InlineData("uuid")]
    [InlineData("comment")]
    [InlineData("poweredOn")]
    [InlineData("suspended")]
    [InlineData("registration")]
    [InlineData("version")]
    public async Task IdentityChangesWhileIntentIsWrittenInvalidateThePreviewBeforeDispatch(string change)
    {
        using var f = IdentityFixture();
        var installed = Version;
        var preview = await IdentityPreview(f);
        f.Files.AfterIntent = () =>
        {
            switch (change)
            {
                case "mac": File.WriteAllText(f.Path, File.ReadAllText(f.Path).Replace(IdentityMac, "00:50:56:00:00:02", StringComparison.Ordinal)); break;
                case "uuid": File.WriteAllText(f.Path, File.ReadAllText(f.Path).Replace(IdentityUuid, IdentityUuid[..^2] + "ef", StringComparison.Ordinal)); break;
                case "comment": File.AppendAllText(f.Path, "# new external comment\n"); break;
                case "registration": f.Server.Id = "other-vm"; break;
                case "version": installed = new(17, 5, 2, 23775572); break;
                default: f.Server.Power = change; break;
            }
        };
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager(() => installed).ApplyAsync(preview, preview.Id, Token));
        Assert.False(error.OutcomeUnknown);
        Assert.EndsWith(".not-sent", error.JournalPath);
        Assert.True(File.Exists(error.JournalPath));
        Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(error.BackupPath, Token));
        Assert.Equal(0, f.Server.PutCount);
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("disconnect")]
    [InlineData("malformed")]
    [InlineData("server")]
    [InlineData("external")]
    [InlineData("started")]
    [InlineData("noChange")]
    [InlineData("wrongReply")]
    [InlineData("otherIdentity")]
    [InlineData("numericSideEffect")]
    [InlineData("inlineComment")]
    [InlineData("quotedInlineCommentAdded")]
    [InlineData("quotedInlineCommentChanged")]
    [InlineData("quotedInlineCommentRemoved")]
    [InlineData("partialConflict")]
    public async Task IdentityUnconfirmedResponsesOrSideEffectsRetainEvidenceAndBlockReplay(string failure)
    {
        using var f = failure is "quotedInlineCommentChanged" or "quotedInlineCommentRemoved"
            ? new Fixture(Encoding.UTF8.GetBytes(ExistingIdentity.Replace("unknown.setting = \"retain\"",
                "unknown.setting = \"retain\" # comment \"original\"", StringComparison.Ordinal)))
            : IdentityFixture();
        var preview = await IdentityPreview(f);
        f.Server.Failure = failure;
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.True(error.OutcomeUnknown);
        Assert.True(VmxApiConfigManager.HasPendingOperation(f.Path));
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(error.BackupPath, Token));
        Assert.Equal(preview.Plan.GetProposedBytes(), await File.ReadAllBytesAsync(error.ProposedPath, Token));
        var pending = await File.ReadAllBytesAsync(error.JournalPath, Token);
        var observed = await File.ReadAllBytesAsync(f.Path, Token);
        var next = await IdentityPreview(f, IdentityConfigField.BiosUuid);
        var requests = f.Server.Requests.Count;
        await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(next, next.Id, Token));
        Assert.Equal(requests, f.Server.Requests.Count);
        Assert.Equal(1, f.Server.PutCount);
        Assert.Equal(observed, await File.ReadAllBytesAsync(f.Path, Token));
        Assert.Equal(pending, await File.ReadAllBytesAsync(error.JournalPath, Token));
        Assert.Equal(0, f.Files.ReplaceCount);
    }

    [Fact]
    public async Task IdentityLateCommitAfterClientTimeoutNeverReplaysTheWrite()
    {
        using var f = IdentityFixture();
        var preview = await IdentityPreview(f);
        f.Server.Failure = "late";
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.True(error.OutcomeUnknown);
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(f.Path, Token));
        f.Server.ReleaseLate.TrySetResult();
        await f.Server.DelayedCommit!;
        var next = await IdentityPreview(f);
        await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(next, next.Id, Token));
        Assert.Equal(1, f.Server.PutCount);
        Assert.True(VmxApiConfigManager.HasPendingOperation(f.Path));
    }

    [Fact]
    public async Task NumericAndIdentityTransactionsShareTheSameLease()
    {
        using var f = IdentityFixture();
        var identity = await IdentityPreview(f);
        var numeric = await f.Preview();
        f.Server.HoldPut = true;
        var first = f.Manager().ApplyAsync(identity, identity.Id, Token);
        await f.Server.EnteredPut.Task.WaitAsync(Token);
        try { await Assert.ThrowsAsync<VmxApiBusyException>(() => f.Manager().ApplyAsync(numeric, numeric.Id, Token)); }
        finally { f.Server.ReleasePut.TrySetResult(); }
        await first;
        Assert.Equal(1, f.Server.PutCount);
    }

    [Fact]
    public async Task IdentityHistorySaveCanBeRetriedWithoutRepeatingApiWrite()
    {
        using var f = IdentityFixture();
        var preview = await IdentityPreview(f, IdentityConfigField.BiosUuid);
        var failure = await Assert.ThrowsAsync<VmxHistoryPersistenceException>(() => new VmxConfigWorkflow(f.Manager(), new FailingHistory())
            .ApplyAsync(preview, preview.Id, false, Token));
        var history = new LocalVmxBackupHistory(Path.Combine(f.Folder, "history"));
        await history.SaveAsync(failure.Entry, Token);
        Assert.Equal(failure.Entry, Assert.Single((await history.ReadAsync(Token)).Entries));
        Assert.Equal(1, f.Server.PutCount);
        Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
    }

    [Theory]
    [InlineData("backup")]
    [InlineData("otherIdentity")]
    [InlineData("related")]
    [InlineData("changedIdentity")]
    [InlineData("mixedReceipt")]
    public async Task IdentityRestoreRejectsTamperedBackupRelatedChangesOrAmbiguousReceipt(string conflict)
    {
        using var f = IdentityFixture();
        var preview = await IdentityPreview(f);
        var receipt = await f.Manager().ApplyAsync(preview, preview.Id, Token);
        switch (conflict)
        {
            case "backup": await File.AppendAllTextAsync(receipt.BackupPath, "# tampered\n", Token); break;
            case "otherIdentity": await File.WriteAllTextAsync(f.Path, (await File.ReadAllTextAsync(f.Path, Token)).Replace(IdentityUuid, IdentityUuid[..^2] + "ef", StringComparison.Ordinal), Token); break;
            case "related": await File.AppendAllTextAsync(f.Path, "ethernet0.checkMACAddress = \"FALSE\"\n", Token); break;
            case "changedIdentity": await File.WriteAllTextAsync(f.Path, (await File.ReadAllTextAsync(f.Path, Token)).Replace(preview.Plan.Changes[0].After, IdentityMac, StringComparison.Ordinal), Token); break;
            case "mixedReceipt": receipt = receipt with { BeforeSettings = new(6144, 4, 2, 19) }; break;
        }
        var observed = await File.ReadAllBytesAsync(f.Path, Token);
        if (conflict == "mixedReceipt")
            await Assert.ThrowsAsync<InvalidDataException>(() => VmxApiConfigManager.PrepareRestoreAsync(f.Path, receipt, Token));
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => VmxApiConfigManager.PrepareRestoreAsync(f.Path, receipt, Token));
        Assert.Equal(observed, await File.ReadAllBytesAsync(f.Path, Token));
        Assert.Equal(1, f.Server.PutCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    public async Task IdentityExplicitRejectionRequiresIndependentUnchangedProofWithoutRetry(HttpStatusCode status)
    {
        using var f = IdentityFixture();
        f.Server.RejectPut = status;
        var preview = await IdentityPreview(f);
        var error = await Assert.ThrowsAsync<VmxApiTransactionException>(() => f.Manager().ApplyAsync(preview, preview.Id, Token));
        Assert.False(error.OutcomeUnknown);
        Assert.EndsWith(".rejected", error.JournalPath);
        Assert.False(VmxApiConfigManager.HasPendingOperation(f.Path));
        Assert.Equal(f.Original, await File.ReadAllBytesAsync(f.Path, Token));
        Assert.Equal(1, f.Server.PutCount);
    }

    [Fact]
    public async Task ExistingNumericHistoryKeepsItsSchemaTwoShapeWithoutIdentityProperties()
    {
        using var f = new Fixture();
        var path = Path.Combine(f.Folder, "history");
        var preview = await f.Preview();
        var entry = await new VmxConfigWorkflow(f.Manager(), new LocalVmxBackupHistory(path)).ApplyAsync(preview, preview.Id, false, Token);
        using var json = JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(path, entry.Id.ToString("N") + ".json"), Token));
        Assert.Equal(2, json.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.False(json.RootElement.TryGetProperty("beforeIdentity", out _));
        Assert.False(json.RootElement.TryGetProperty("afterIdentity", out _));
        Assert.Equal(entry, Assert.Single((await new LocalVmxBackupHistory(path).ReadAsync(Token)).Entries));
    }

    [Theory]
    [InlineData("oldSchema")]
    [InlineData("missingAfter")]
    [InlineData("mixedNumeric")]
    [InlineData("twoFields")]
    [InlineData("relatedHash")]
    public async Task InvalidIdentityHistoryIsReportedAndRetainedWithoutEnablingRestore(string corruption)
    {
        using var f = IdentityFixture();
        var historyPath = Path.Combine(f.Folder, "history");
        var preview = await IdentityPreview(f);
        var entry = await new VmxConfigWorkflow(f.Manager(), new LocalVmxBackupHistory(historyPath)).ApplyAsync(preview, preview.Id, false, Token);
        var recordPath = Path.Combine(historyPath, entry.Id.ToString("N") + ".json");
        var json = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllBytesAsync(recordPath, Token))!;
        switch (corruption)
        {
            case "oldSchema": json["schemaVersion"] = 2; break;
            case "missingAfter": json["afterIdentity"] = null; break;
            case "mixedNumeric": json["beforeSettings"] = System.Text.Json.Nodes.JsonNode.Parse("{\"memoryMiB\":6144,\"processors\":4,\"coresPerSocket\":2,\"hardwareVersion\":19}"); break;
            case "twoFields": json["afterIdentity"]!["biosUuid"] = IdentityUuid[..^2] + "ef"; break;
            case "relatedHash": json["afterIdentity"]!["relatedHash"] = new string('A', 64); break;
        }
        await File.WriteAllTextAsync(recordPath, json.ToJsonString(), Token);
        var bytes = await File.ReadAllBytesAsync(recordPath, Token);
        var reopened = await new LocalVmxBackupHistory(historyPath).ReadAsync(Token);
        Assert.Empty(reopened.Entries);
        Assert.Equal(1, reopened.UnreadableFiles);
        Assert.Equal(bytes, await File.ReadAllBytesAsync(recordPath, Token));
        Assert.Equal(1, f.Server.PutCount);
    }

    private sealed partial class Server
    {
        private HttpResponseMessage IdentitySetting(string uri)
        {
            var name = uri.Split('/')[^1];
            Assert.True(name is "ethernet0.address" or "uuid.bios");
            var values = VmxDocument.Parse(File.ReadAllBytes(path)).Entries.ToDictionary(item => item.Key, item => item.Value);
            return Reply(new { name, value = Failure == "wrongReply" && PutCount > 0 && name == "ethernet0.address" ? "00:50:56:00:00:03" : values[name] });
        }

        private async Task<HttpResponseMessage> UpdateIdentityAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.Equal("application/vnd.vmware.vmw.rest-v1+json", request.Content!.Headers.ContentType!.ToString());
            var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(await request.Content.ReadAsStringAsync(token))!;
            Assert.Equal(2, payload.Count);
            var key = payload["name"];
            Assert.True(key is "ethernet0.address" or "uuid.bios");
            var text = await File.ReadAllTextAsync(path, token);
            if (Failure != "noChange")
            {
                // Reproduce the verified vmrest behavior: replace one existing value,
                // move that line to the end and normalize line endings.
                var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
                    .Where(line => line.Length > 0 && !line.StartsWith(key + " =", StringComparison.Ordinal)).ToList();
                lines.Add(key + " = \"" + payload["value"] + "\"");
                text = string.Join("\r\n", lines) + "\r\n";
            }
            if (Failure == "late")
            {
                DelayedCommit = CommitLaterAsync(text, Token);
                throw new TaskCanceledException("Synthetic identity timeout before commit");
            }
            await File.WriteAllTextAsync(path, text, token);
            AfterPut?.Invoke();
            if (Failure == "timeout") throw new TaskCanceledException("Synthetic identity timeout");
            if (Failure == "disconnect") throw new HttpRequestException("Synthetic identity disconnect");
            if (Failure == "external") await File.AppendAllTextAsync(path, "# concurrent external edit\r\n", token);
            if (Failure == "inlineComment") await File.WriteAllTextAsync(path, text.Replace("unknown.setting = \"retain\"", "unknown.setting = \"retain\" # external comment", StringComparison.Ordinal), token);
            if (Failure == "quotedInlineCommentAdded") await File.WriteAllTextAsync(path, text.Replace("unknown.setting = \"retain\"", "unknown.setting = \"retain\" # external \"quoted\"", StringComparison.Ordinal), token);
            if (Failure == "quotedInlineCommentChanged") await File.WriteAllTextAsync(path, text.Replace("# comment \"original\"", "# changed \"different\"", StringComparison.Ordinal), token);
            if (Failure == "quotedInlineCommentRemoved") await File.WriteAllTextAsync(path, text.Replace(" # comment \"original\"", "", StringComparison.Ordinal), token);
            if (Failure == "numericSideEffect") await File.WriteAllTextAsync(path, text.Replace("6144", "4096", StringComparison.Ordinal), token);
            if (Failure == "otherIdentity") await File.WriteAllTextAsync(path, text.Replace(IdentityUuid, IdentityUuid[..^2] + "ef", StringComparison.Ordinal), token);
            if (Failure == "started") Power = "poweredOn";
            if (Failure == "partialConflict") return Reply(new { message = "synthetic partial mutation" }, HttpStatusCode.Conflict);
            if (Failure == "server") return Reply(new { message = "synthetic server failure" }, HttpStatusCode.InternalServerError);
            if (Failure == "malformed") return new(HttpStatusCode.OK) { Content = new StringContent("not JSON") };
            return new(HttpStatusCode.OK) { Content = new ByteArrayContent([]) };
        }
    }
}
