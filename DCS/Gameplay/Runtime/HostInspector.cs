using System.Text;

namespace DCS.Core
{
    public static class HostInspector
    {
        /// <summary>
        /// Формирует отформатированный текст со всеми компонентами хоста.
        /// </summary>
        public static void InspectHost(Host host, HostChain chain, StringBuilder sb)
        {
            if (!HostManager.IsValid(host))
            {
                sb.AppendLine($"[Host {host.Id}] INVALID");
                return;
            }

            sb.Append("Host #").Append(host.Id)
              .Append(" (gen ").Append(host.Generation).AppendLine(")");

            if (chain == null)
            {
                sb.AppendLine("  (no chain)");
                return;
            }

            int currentIndex = HostManager.GlobalHosts[host.Id].FirstComponent;
            while (currentIndex >= 0)
            {
                ref ChainNode node = ref chain.GetNodeByIndex(currentIndex);
                int typeId = node.TypeId;
                string typeName = ComponentRegistry.GetTypeNameById(typeId);

                sb.Append("  ").Append(typeName)
                  .Append(" handle=").Append(node.Component.Id)
                  .Append('/').Append(node.Component.Generation)
                  .AppendLine();

                currentIndex = node.Next;
            }
        }

        /// <summary>
        /// Формирует текст конкретного компонента по типу и хосту.
        /// </summary>
        public static string InspectComponent(Host host, HostChain chain, int typeId)
        {
            if (!HostManager.IsValid(host) || chain == null) return null;

            ChainNode node = chain.GetTypedHandle(host, typeId);
            if (node.IsNull) return null;

            string typeName = ComponentRegistry.GetTypeNameById(typeId);
            var sb = new StringBuilder();
            sb.Append(typeName)
              .Append(" handle=").Append(node.Component.Id)
              .Append('/').Append(node.Component.Generation);

            // Тут можно расширить: чтение конкретных полей через JIT-делегат
            // Но пока — только typeName и handle.

            return sb.ToString();
        }
    }
}