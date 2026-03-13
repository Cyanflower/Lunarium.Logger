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

namespace Lunarium.Logger.GlobalConfigExtensions
{
    namespace Lunarium.Logger.GlobalConfigExtensions.AtomicOps
    {
        public static class AtomicOpsGlobalConfigExtensions
        {
            /// <summary>
            /// <para>启用 BufferWriter 的 Dispose 方法使用 Interlocked 操作来确保线程安全。这可以防止在多线程环境中出现竞争条件和资源泄漏问题。</para>
            /// <para>当启用此选项时，BufferWriter 的 Dispose 方法将使用 Interlocked，这将引入一个较重的 Interlocked.Exchange 多线程冻结操作。</para>
            /// <para>默认为 Disabled（禁用），以避免不必要的性能开销，除非你在极端多线程并发竞争环境中，确实出现了日志库内部所使用的 BufferWriter 重复多次 Dispose 造成了 _buffer 多次归还 ArrayPool 的问题。</para>
            /// </summary>
            /// <param name="builder"></param>
            /// <returns></returns>
            public static GlobalConfigurator.ConfigurationBuilder EnableBufferWriterInterlocked(this GlobalConfigurator.ConfigurationBuilder builder)
            {
                AtomicOpsConfig.EnableBufferWriterInterlocked();
                return builder;
            }
            
            public static GlobalConfigurator.ConfigurationBuilder DisableBufferWriterInterlocked(this GlobalConfigurator.ConfigurationBuilder builder)
            {
                AtomicOpsConfig.DisableBufferWriterInterlocked();
                return builder;
            }
        }
    }
    namespace Lunarium.Logger.GlobalConfigExtensions.SafetyClear
    {
        public static class SafetyClearGlobalConfigExtensions
        {
            /// <summary>
            /// <para>启用内存数组安全清空操作。</para>
            /// <para>当启用此选项时，内部实现的 LogWriter 会在归池或释放时清空在内存中所持有的 byte[] buffer，而不仅仅是重置数组索引。</para>
            /// <para>注: 该配置仅针对 LogWriter 在内存中所持有的 byte 数组，避免 ArrayPool 数组幽灵数据泄露导致数组内容错乱问题，与敏感信息和日志输出目标（如文件、控制台等）无关，实际日志落盘内容需要用户自行保证处理敏感信息。</para>
            /// <para>默认为 false（禁用），以避免不必要的性能开销，除非项目或引用库中有对 ArrayPool 的调用且对租借的数组的索引操作并不安全或有漏洞</para>
            /// </summary>
            /// <param name="builder"></param>
            /// <returns></returns>
            public static GlobalConfigurator.ConfigurationBuilder EnableSafetyClear(this GlobalConfigurator.ConfigurationBuilder builder)
            {
                SafetyClearConfig.EnableSafetyClear();
                return builder;
            }
            
            public static GlobalConfigurator.ConfigurationBuilder DisableSafetyClear(this GlobalConfigurator.ConfigurationBuilder builder)
            {
                SafetyClearConfig.DisableSafetyClear();
                return builder;
            }
        }
    }
}

