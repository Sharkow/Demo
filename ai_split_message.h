#pragma once

#include "iface_remote_system_settings.h"
#include "dvm_ai_msg.h"
#include "ai_mes_send.h"

namespace libair {

std::string MakeMessageText(const aim::OutMsg& messageToSend, const AptInfo& aptInfo);

std::vector<aim::OutMsg> SplitLongMessage(
        const aim::Message& origMessage,
        const AptInfo& aptInfo,
        const ipnr::Pnr& oldPnr,
        const ipnr::Pnr& newPnr,
        const boost::optional<libair::EotrDivInfo>& pnrDivisionInfo);

} /* namespace libair */
