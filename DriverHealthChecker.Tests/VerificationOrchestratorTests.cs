using System.Collections.Generic;
using DriverHealthChecker.App;
using Xunit;

namespace DriverHealthChecker.Tests;

public sealed class VerificationOrchestratorTests
{
    [Fact]
    public void SelectVerifier_ChoosesMatchingVerifier()
    {
        var matchingVerifier = new ProbeVendorDriverVerifier(canHandle: true, source: "matching");
        var orchestrator = new VerificationOrchestrator(
            new IVendorDriverVerifier[]
            {
                new ProbeVendorDriverVerifier(canHandle: false, source: "first"),
                matchingVerifier
            });

        var verifier = orchestrator.SelectVerifier(new DriverIdentity { NormalizedManufacturer = "NVIDIA CORPORATION" });

        Assert.Same(matchingVerifier, verifier);
    }

    [Fact]
    public void Verify_WithMatchingVendorVerifier_ReturnsStubVerifierResult()
    {
        var orchestrator = new VerificationOrchestrator(
            new IVendorDriverVerifier[]
            {
                new NvidiaDriverVerifier(),
                new IntelDriverVerifier(),
                new AmdDriverVerifier()
            });

        var result = orchestrator.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_10DE&DEV_2704"
        });

        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, result.Status);
        Assert.Equal(VerificationSourceType.Unknown, result.VerificationSourceType);
        Assert.Equal("NVIDIA official verification (stub)", result.SourceDetails);
        Assert.Equal(VerificationFailureReasonType.VerifierNotImplemented, result.FailureReasonType);
        Assert.Contains("stub", result.EvidenceSummary);
    }

    [Fact]
    public void Verify_UnknownIdentity_UsesUnableToVerifyFallback()
    {
        var orchestrator = new VerificationOrchestrator(
            new IVendorDriverVerifier[]
            {
                new NvidiaDriverVerifier(),
                new IntelDriverVerifier(),
                new AmdDriverVerifier()
            });

        var result = orchestrator.Verify(new DriverIdentity
        {
            NormalizedManufacturer = "CONTOSO"
        });

        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, result.Status);
        Assert.Equal(VerificationSourceType.Unknown, result.VerificationSourceType);
        Assert.Equal("VerificationOrchestrator fallback", result.SourceDetails);
        Assert.Equal(VerificationFailureReasonType.NoMatchingVendor, result.FailureReasonType);
        Assert.Contains("No vendor verifier", result.FailureReason);
    }

    [Fact]
    public void Verify_NoRegisteredVerifiers_ReturnsUnableToVerifyReliably()
    {
        var orchestrator = new VerificationOrchestrator(new List<IVendorDriverVerifier>());

        var result = orchestrator.Verify(new DriverIdentity
        {
            PnpDeviceId = @"PCI\VEN_8086&DEV_51F0"
        });

        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, result.Status);
        Assert.Equal(VerificationSourceType.Unknown, result.VerificationSourceType);
        Assert.Equal("VerificationOrchestrator fallback", result.SourceDetails);
        Assert.Equal(VerificationFailureReasonType.NoMatchingVendor, result.FailureReasonType);
        Assert.Contains("could not match the identity", result.EvidenceSummary);
    }

    [Fact]
    public void Verify_DoesNotFallbackToHeuristicsForContainsLikeManufacturer()
    {
        var orchestrator = new VerificationOrchestrator(
            new IVendorDriverVerifier[]
            {
                new NvidiaDriverVerifier(),
                new IntelDriverVerifier(),
                new AmdDriverVerifier()
            });

        var result = orchestrator.Verify(new DriverIdentity
        {
            NormalizedManufacturer = "SUPER NVIDIA COMPATIBLE DEVICES"
        });

        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, result.Status);
        Assert.Equal(VerificationFailureReasonType.NoMatchingVendor, result.FailureReasonType);
        Assert.Equal("VerificationOrchestrator fallback", result.SourceDetails);
        Assert.Contains("could not match the identity", result.EvidenceSummary);
    }

    [Fact]
    public void Verify_WhenVerifierThrows_ReturnsVerificationFailed()
    {
        var orchestrator = new VerificationOrchestrator(
            new IVendorDriverVerifier[]
            {
                new ThrowingVendorDriverVerifier()
            });

        var result = orchestrator.Verify(new DriverIdentity());

        Assert.Equal(DriverVerificationStatus.UnableToVerifyReliably, result.Status);
        Assert.Equal(VerificationFailureReasonType.VerificationFailed, result.FailureReasonType);
        Assert.Equal("VerificationOrchestrator fallback", result.SourceDetails);
        Assert.Contains("Verifier routing failed", result.EvidenceSummary);
    }

    private sealed class ProbeVendorDriverVerifier : IVendorDriverVerifier
    {
        private readonly bool _canHandle;
        private readonly string _source;

        public ProbeVendorDriverVerifier(bool canHandle, string source)
        {
            _canHandle = canHandle;
            _source = source;
        }

        public bool CanHandle(DriverIdentity identity) => _canHandle;

        public DriverVerificationResult Verify(DriverIdentity identity)
        {
            return new DriverVerificationResult
            {
                Status = DriverVerificationStatus.UnableToVerifyReliably,
                VerificationSourceType = VerificationSourceType.Unknown,
                SourceDetails = _source,
                FailureReasonType = VerificationFailureReasonType.VerifierNotImplemented,
                EvidenceSummary = "probe"
            };
        }
    }

    private sealed class ThrowingVendorDriverVerifier : IVendorDriverVerifier
    {
        public bool CanHandle(DriverIdentity identity) => true;

        public DriverVerificationResult Verify(DriverIdentity identity)
        {
            throw new System.InvalidOperationException("stub");
        }
    }
}
