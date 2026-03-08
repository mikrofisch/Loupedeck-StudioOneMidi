namespace Loupedeck.StudioOneMidiPlugin.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public class Midi2Connection
    {
        private readonly List<EndpointPair> _endpointPairs = new();

        public class EndpointPair
        {
            public String Name { get; }
            public String EndpointDeviceIdA { get; }
            public String EndpointDeviceIdB { get; }
            public Guid AssociationIdA { get; }
            public Guid AssociationIdB { get; }

            public EndpointPair(String name, Guid associationIdA, String endpointDeviceIdA, Guid associationIdB, String endpointDeviceIdB)
            {
                this.Name = name;
                this.AssociationIdA = associationIdA;
                this.EndpointDeviceIdA = endpointDeviceIdA;
                this.AssociationIdB = associationIdB;
                this.EndpointDeviceIdB = endpointDeviceIdB;
            }
        }

        private String GetCreatorExePath()
        {
            var exePath = Path.Combine(Environment.CurrentDirectory, "Midi2EndpointCreator.exe");

            if (!File.Exists(exePath))
            {
                throw new FileNotFoundException($"Midi2EndpointCreator.exe not found at: {exePath}");
            }

            return exePath;
        }

        public EndpointPair? CreateEndpointPair(String endpointName)
        {
            if (String.IsNullOrWhiteSpace(endpointName))
            {
                throw new ArgumentException("Endpoint base name must not be empty.", nameof(endpointName));
            }

            var exePath = this.GetCreatorExePath();

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"create \"{endpointName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Debug.WriteLine("Midi2Connection: Failed to start Midi2EndpointCreator process.");
                return null;
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(10000);

            if (!String.IsNullOrEmpty(stderr))
            {
                Debug.WriteLine($"Midi2Connection: {stderr.Trim()}");
            }

            if (process.ExitCode != 0)
            {
                Debug.WriteLine($"Midi2Connection: Process exited with code {process.ExitCode}.");
                return null;
            }

            // Parse: OK|<endpointName>|<associationIdA>|<endpointDeviceIdA>|<associationIdB>|<endpointDeviceIdB>
            var line = stdout.Trim();
            var parts = line.Split('|');

            if (parts.Length < 6 || (parts[0] != "OK" && parts[0] != "EXIST"))
            {
                Debug.WriteLine($"Midi2Connection: Unexpected output: {line}");
                return null;
            }

            var pair = new EndpointPair(
                parts[1],
                Guid.Parse(parts[2]),
                parts[3],
                Guid.Parse(parts[4]),
                parts[5]
            );

            this._endpointPairs.Add(pair);
            return pair;
        }

        public Boolean RemoveEndpointPair(EndpointPair pair)
        {
            var exePath = this.GetCreatorExePath();
            var success = true;

            // Remove both endpoints individually
            foreach (var associationId in new[] { pair.AssociationIdA, pair.AssociationIdB })
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"remove \"{associationId}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    success = false;
                    continue;
                }

                process.WaitForExit(10000);

                if (process.ExitCode != 0)
                {
                    Debug.WriteLine($"Midi2Connection: Failed to remove endpoint with association ID {associationId}.");
                    success = false;
                }
            }

            if (success)
            {
                this._endpointPairs.Remove(pair);
            }

            return success;
        }

        public void RemoveAllEndpoints()
        {
            foreach (var pair in this._endpointPairs.ToArray())
            {
                this.RemoveEndpointPair(pair);
            }
        }
    }
}