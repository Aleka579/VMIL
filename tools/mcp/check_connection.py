"""Verify MCP initialization and tool discovery without calling any VM tool."""

import asyncio
import json
import sys

from mcp import ClientSession
from mcp.client.stdio import StdioServerParameters, stdio_client


READ_TOOLS = {
    "vmrun_list",
    "vmrun_tools_state",
    "vmrun_guest_ip",
    "vmrun_file_exists",
    "vmrun_dir_exists",
    "vmrun_ls",
    "vmrun_ps",
}


async def main() -> None:
    parameters = StdioServerParameters(
        command=sys.executable,
        args=["-I", "-m", "vmware_mcp.server"],
    )
    async with asyncio.timeout(30):
        async with stdio_client(parameters) as (reader, writer):
            async with ClientSession(reader, writer) as session:
                initialized = await session.initialize()
                catalog = await session.list_tools()
                available = {tool.name for tool in catalog.tools}
                missing = sorted(READ_TOOLS - available)
                if missing:
                    raise RuntimeError(f"Configured read tools are unavailable: {missing}")
                print(json.dumps({
                    "server": initialized.serverInfo.name,
                    "server_version": initialized.serverInfo.version,
                    "protocol": initialized.protocolVersion,
                    "tool_count": len(catalog.tools),
                    "configured_read_tools": sorted(READ_TOOLS),
                    "tool_calls": 0,
                    "v01_tests_started": False,
                }, ensure_ascii=False))


if __name__ == "__main__":
    asyncio.run(main())
