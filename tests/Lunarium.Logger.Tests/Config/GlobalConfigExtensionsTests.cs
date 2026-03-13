// Copyright 2026 Cyanflower
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Reflection;
using Lunarium.Logger.GlobalConfig;
using Lunarium.Logger.GlobalConfigExtensions.AtomicOps;
using Lunarium.Logger.GlobalConfigExtensions.SafetyClear;
using Xunit;
using FluentAssertions;

namespace Lunarium.Logger.Tests.Config;

[Collection("GlobalConfigurator")] // Use the same collection as GlobalConfiguratorTests to avoid static conflicts
public class GlobalConfigExtensionsTests
{
    private static void ResetAll()
    {
        GlobalConfigLock.Configured = false;
        
        typeof(GlobalConfigurator).GetField("_isConfiguring", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, false);
            
        typeof(AtomicOpsConfig).GetField("BufferWriterDisposeInterlocked", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, false);
            
        typeof(SafetyClearConfig).GetField("SafetyClear", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, false);
    }

    [Fact]
    public void ConfigurationBuilder_EnableBufferWriterInterlocked_SetsConfig()
    {
        ResetAll();
        GlobalConfigurator.Configure()
            .EnableBufferWriterInterlocked()
            .Apply();
            
        AtomicOpsConfig.BufferWriterDisposeInterlocked.Should().BeTrue();
        ResetAll();
    }

    [Fact]
    public void ConfigurationBuilder_DisableBufferWriterInterlocked_SetsConfig()
    {
        ResetAll();
        // First enable then disable
        GlobalConfigurator.Configure()
            .EnableBufferWriterInterlocked()
            .DisableBufferWriterInterlocked()
            .Apply();
            
        AtomicOpsConfig.BufferWriterDisposeInterlocked.Should().BeFalse();
        ResetAll();
    }

    [Fact]
    public void ConfigurationBuilder_EnableSafetyClear_SetsConfig()
    {
        ResetAll();
        GlobalConfigurator.Configure()
            .EnableSafetyClear()
            .Apply();
            
        SafetyClearConfig.SafetyClear.Should().BeTrue();
        ResetAll();
    }

    [Fact]
    public void ConfigurationBuilder_DisableSafetyClear_SetsConfig()
    {
        ResetAll();
        // First enable then disable
        GlobalConfigurator.Configure()
            .EnableSafetyClear()
            .DisableSafetyClear()
            .Apply();
            
        SafetyClearConfig.SafetyClear.Should().BeFalse();
        ResetAll();
    }
}
