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

using Lunarium.Logger.Target;
using Lunarium.Logger.Filter;

namespace Lunarium.Logger;

/// <summary>
/// 将一个 ILogTarget 与其特定的 SinkOutputConfig 绑定在一起。
/// 这个结构体封装了一个配置好的、准备就绪的日志输出目标。
/// </summary>
/// <param name="Target">日志输出目标实例。</param>
/// <param name="Configuration">此 Sink 的配置。</param>
internal readonly record struct Sink
{
    internal ILogTarget Target { get; }
    internal SinkOutputConfig Configuration { get; }
    internal LoggerFilter LoggerFilter { get; }


    internal Sink(ILogTarget target, SinkOutputConfig configuration)
    {
        Target = target;
        Configuration = configuration;
        LoggerFilter = new LoggerFilter();

        SetTargetProperty();
    }

    internal Sink(ILogTarget target)
    {
        Target = target;
        Configuration = new SinkOutputConfig();
        LoggerFilter = new LoggerFilter();

        SetTargetProperty();
    }

    internal void Emit(LogEntry logEntry)
    {
        if (LoggerFilter.ShouldEmit(logEntry, Configuration))
        {
            logEntry.ParseMessage();
            Target.Emit(logEntry);
        }
    }

    internal void Deconstruct(out ILogTarget target, out SinkOutputConfig configuration)
    {
        target = Target;
        configuration = Configuration;
    }
    
    internal void Deconstruct(out ILogTarget target, out SinkOutputConfig configuration, out LoggerFilter loggerFilter)
    {
        target = Target;
        configuration = Configuration;
        loggerFilter = LoggerFilter;
    }

    private void SetTargetProperty()
    {
        if (Target is IJsonTextTarget jsonSink && Configuration.ToJson is not null)
        {
            jsonSink.ToJson = Configuration.ToJson.Value;
        }
        if (Target is IColorTextTarget colorSink && Configuration.IsColor is not null)
        {
            colorSink.IsColor = Configuration.IsColor.Value;
        }
    }
}
