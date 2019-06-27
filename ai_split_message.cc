#include "ai_split_message.h"

#include "ai_send_passengers.h"

#include <serverlib/algo.h>

#define NICKNAME "M_AKULOV"
#include <serverlib/slogger.h>

namespace libair {

std::string MakeMessageText(const aim::OutMsg& messageToSend, const AptInfo& aptInfo)
{
    return messageToSend.makeText(false,
                                  ENGLISH,
                                  aptInfo.bcfg()->translit() ? ENGLISH : RUSSIAN,
                                  aptInfo.cfg().rusRecloc() ? RUSSIAN : ENGLISH,
                                  aptInfo.cfg().combineSurnames(),
                                  aptInfo.bcfg()->censor(),
                                  aptInfo.bcfg()->translit());
}

static bool MessageExceedsMaxLength(const aim::OutMsg& messageToSend, const AptInfo& aptInfo)
{
    const int ALLOWED_NUMBER_OF_LINES = 40;
    const std::string messageText = MakeMessageText(messageToSend, aptInfo);
    return std::count(messageText.begin(), messageText.end(), '\n') > ALLOWED_NUMBER_OF_LINES;
}

struct AllServices
{
    std::vector<aim::SSVC> ssvcs;
    std::vector<boost::variant<aim::SSR, aim::ASVC> > ssrAsvcs;
    std::vector<aim::OSI> osis;

    bool Empty() const { return ssvcs.empty() && ssrAsvcs.empty() && osis.empty(); }
};

struct SplitServicesInfo
{
    AllServices split;// Услуги, которые надо поместить в следующую телеграмму
    AllServices left;// Услуги, которые ещё останется отправить
};

struct SplitMsgInfo
{
    aim::OutMsg message;
    AllServices leftServices;
};

static bool IsRlocOsi(const aim::OSI& osi)
{
    return (osi.text().find("RLOC") == 0);
}

static bool IsTcpOsi(const aim::OSI& osi)
{
    return osi.tcpOsiNumberOfNames().is_initialized();
}

static bool IsRequiredOsi(const aim::OSI& osi)
{
    return IsRlocOsi(osi)
        || IsTcpOsi(osi);
}

static bool IsGrpsSsr(const boost::variant<aim::SSR, aim::ASVC>& ssrOrAsvc)
{
    if (const aim::SSR* ssr = boost::get<aim::SSR>(&ssrOrAsvc)) {
        return ssr->code() == BaseTables::Ssrcode("GRPS");
    }
    else return false;
}

static bool IsExtraSeatSsr(const boost::variant<aim::SSR, aim::ASVC>& ssrOrAsvc)
{
    const BaseTables::Ssrcode exstSsr("EXST");
    const BaseTables::Ssrcode stcrSsr("STCR");
    const BaseTables::Ssrcode cbbgSsr("CBBG");
    if (const aim::SSR* ssr = boost::get<aim::SSR>(&ssrOrAsvc)) {
        return ssr->code() == exstSsr
            || ssr->code() == stcrSsr
            || ssr->code() == cbbgSsr;
    }
    else return false;
}

static bool IsChldInftSsr(const boost::variant<aim::SSR, aim::ASVC>& ssrOrAsvc)
{
    const BaseTables::Ssrcode inftSsr("INFT");
    const BaseTables::Ssrcode chldSsr("CHLD");
    if (const aim::SSR* ssr = boost::get<aim::SSR>(&ssrOrAsvc)) {
        return ssr->code() == inftSsr
            || ssr->code() == chldSsr;
    }
    else return false;
}

static boost::optional<aim::SSR> FindGrpsSsr(const std::vector<boost::variant<aim::SSR, aim::ASVC> >& ssrAsvcs)
{
    if (const auto& ssr = algo::find_opt_if<boost::optional>(ssrAsvcs, [](const boost::variant<aim::SSR, aim::ASVC>& serv)
                                                                       { return IsGrpsSsr(serv); }))
    {
        return boost::get<aim::SSR>(*ssr);
    }
    else return boost::none;
}

static boost::optional<aidb::SegmentKey> GetSegmentKey(const boost::variant<aim::SSR, aim::ASVC>& ssrOrAsvc)
{
     if (const aim::SSR* ssr = boost::get<aim::SSR>(&ssrOrAsvc)) {
         if (ssr->seg()) return ssr->seg()->toSegmentKey(ssr->airline());
         else return boost::none;
     }
     else {
         const aim::ASVC* asvc = boost::get<aim::ASVC>(&ssrOrAsvc);
         return asvc->seg().toSegmentKey(asvc->airline());
     }
}

static bool IsLinkedToInactiveSegment(const boost::variant<aim::SSR, aim::ASVC>& ssrOrAsvc, const aim::Message& origMessage)
{
    if (const boost::optional<aidb::SegmentKey> ssrSegmentKey = GetSegmentKey(ssrOrAsvc)) {
        if (const aim::Segment::ConstPtr_t msgSegment = origMessage.findSegment(*ssrSegmentKey)) {
            return IsCancelled(msgSegment->actionCode());
        }
    }
    return false;
}

static SplitServicesInfo SplitRequiredServicesForFirstMessage(const aim::Message& origMessage)
{
    SplitServicesInfo result;
    result.left.ssvcs = origMessage.ssvcs();
    for (const boost::variant<aim::SSR, aim::ASVC>& ssrOrAsvc : origMessage.ssrAsvcs())
    {
        if(IsGrpsSsr(ssrOrAsvc)
        || IsLinkedToInactiveSegment(ssrOrAsvc, origMessage)
        || IsExtraSeatSsr(ssrOrAsvc)
        || IsChldInftSsr(ssrOrAsvc))
        {
            result.split.ssrAsvcs.push_back(ssrOrAsvc);
        }
        else result.left.ssrAsvcs.push_back(ssrOrAsvc);
    }
    for (const aim::OSI& osi : origMessage.osis())
    {
        if (IsRequiredOsi(osi)) result.split.osis.push_back(osi);
        else result.left.osis.push_back(osi);
    }
    return result;
}

static SplitMsgInfo MakeMinimalFirstMessage(const aim::Message& origMessage)
{
    const SplitServicesInfo splitServices = SplitRequiredServicesForFirstMessage(origMessage);
    return SplitMsgInfo
    {
        aim::OutMsg
        {
            origMessage.addressList(),
            origMessage.cref(),
            origMessage.ids(),
            origMessage.reclocs(),
            origMessage.renamedPassengers(),
            origMessage.passengers(),
            origMessage.segments(),
            origMessage.taxies(),
            splitServices.split.ssvcs,
            splitServices.split.ssrAsvcs,
            origMessage.unknownSsrs(),
            splitServices.split.osis
        },
        splitServices.left
    };
}

/* Такой ключ показывает, что SSR-ASVC !могут быть! связаны, но это не точно.
 * Точного определения привязок мы тут не делаем,
 * нам нужно только определить услуги, которые не следует помещать в разные телеграммы. */
struct SsrAsvcLinkKey
{
    boost::optional<BaseTables::Ssrcode> ssrCode;
    BaseTables::Company airline;
    boost::optional<aim::SSRSegment> segment;
    std::vector<aim::Name> passengers;

    SsrAsvcLinkKey(const aim::SSR& ssr):
        ssrCode(ssr.code()),
        airline(ssr.airline()),
        segment(ssr.seg()),
        passengers(ssr.passengers())
    {}

    SsrAsvcLinkKey(const aim::ASVC& asvc):
        ssrCode(asvc.ssrCode()),
        airline(asvc.airline()),
        segment(asvc.seg()),
        passengers(std::vector<aim::Name>(1, asvc.passenger()))
    {}

    bool Matches(const SsrAsvcLinkKey& other) const
    {
        return this->ssrCode && this->ssrCode == other.ssrCode
            && this->airline == other.airline
            && this->segment && this->segment == other.segment
            && this->passengers.size() == 1 && other.passengers.size() == 1
            && this->passengers.at(0) == other.passengers.at(0);
    }

    bool Matches(const boost::variant<aim::SSR, aim::ASVC>& ssrOrAsvc) const
    {
        if (const aim::SSR* ssr = boost::get<aim::SSR>(&ssrOrAsvc)) {
            return this->Matches(SsrAsvcLinkKey(*ssr));
        }
        else return this->Matches(SsrAsvcLinkKey(*boost::get<aim::ASVC>(&ssrOrAsvc)));
    }
};

static bool MayBeLinked(const aim::SSR& ssr, const aim::ASVC& asvc)
{
    return SsrAsvcLinkKey(ssr).Matches(SsrAsvcLinkKey(asvc));
}

static bool HaveLinkedSsrs(const std::vector<aim::SSR>& ssrs, const aim::ASVC& asvc)
{
    return asvc.ssrCode()
        && algo::find_opt_if<boost::optional>(ssrs, [&asvc](const aim::SSR& ssr) { return MayBeLinked(ssr, asvc); })
            .is_initialized();
}

static bool HaveLinkedAsvcs(const std::vector<aim::ASVC>& asvcs, const aim::SSR& ssr)
{
    return algo::find_opt_if<boost::optional>(asvcs, [&ssr](const aim::ASVC& asvc) { return MayBeLinked(ssr, asvc); })
            .is_initialized();
}

static SplitMsgInfo AddSsrAsvcsByKey(const SplitMsgInfo& current, const SsrAsvcLinkKey& linkKey)
{
    aim::OutMsg resultMsg = current.message;
    std::vector<boost::variant<aim::SSR, aim::ASVC> > leftSsrAsvcs;
    for (const boost::variant<aim::SSR, aim::ASVC>& ssrOrAsvc : current.leftServices.ssrAsvcs)
    {
        if (linkKey.Matches(ssrOrAsvc)) resultMsg.ssrAsvcs.push_back(ssrOrAsvc);
        else leftSsrAsvcs.push_back(ssrOrAsvc);
    }
    return SplitMsgInfo { resultMsg, AllServices { current.leftServices.ssvcs, leftSsrAsvcs, current.leftServices.osis } };
}

static std::string GetTicketNum(const aim::SSR& tkneSsr)
{
    ASSERT(tkneSsr.code() == BaseTables::Ssrcode("TKNE"));
    const std::string& text = tkneSsr.freeText();
    const std::string::size_type couponNumPos = text.find('C');
    if (couponNumPos == std::string::npos) return std::string();
    else return text.substr(0, couponNumPos);
}

static bool IsPartOfSameTicket(const boost::variant<aim::SSR, aim::ASVC>& ssrOrAsvc, const std::string& ticketNum)
{
    if (const aim::SSR* ssr = boost::get<aim::SSR>(&ssrOrAsvc)) {
        if (ssr->code() == BaseTables::Ssrcode("TKNE") && GetTicketNum(*ssr) == ticketNum) {
            return true;
        }
    }
    return false;
}

// Все SSR TKNE, содержащие один и тот же номер билета, должны идти в одной телеграмме
static SplitMsgInfo AddTicket(const SplitMsgInfo& current, const aim::SSR& ticketPart)
{
    const std::string ticketNum = GetTicketNum(ticketPart);
    aim::OutMsg resultMsg = current.message;
    std::vector<boost::variant<aim::SSR, aim::ASVC> > leftSsrAsvcs;
    for (const boost::variant<aim::SSR, aim::ASVC>& ssrOrAsvc : current.leftServices.ssrAsvcs)
    {
        if (IsPartOfSameTicket(ssrOrAsvc, ticketNum)) resultMsg.ssrAsvcs.push_back(ssrOrAsvc);
        else leftSsrAsvcs.push_back(ssrOrAsvc);
    }
    return SplitMsgInfo { resultMsg, AllServices { current.leftServices.ssvcs, leftSsrAsvcs, current.leftServices.osis } };
}

static SplitMsgInfo AddNextSsrToMessage(const SplitMsgInfo& current)
{
    ASSERT(current.leftServices.ssrAsvcs.size() > 0);
    const boost::variant<aim::SSR, aim::ASVC>& nextService = current.leftServices.ssrAsvcs.at(0);
    if (const aim::SSR* ssr = boost::get<aim::SSR>(&nextService)) {
        if (ssr->code() == BaseTables::Ssrcode("TKNE")) {
            return AddTicket(current, *ssr);
        }
        else if (HaveLinkedAsvcs(Asvcs(current.leftServices.ssrAsvcs), *ssr)) {
            return AddSsrAsvcsByKey(current, SsrAsvcLinkKey(*ssr));
        }
        else {
            aim::OutMsg resultMsg = current.message;
            resultMsg.ssrAsvcs.push_back(*ssr);
            const std::vector<boost::variant<aim::SSR, aim::ASVC> > leftSsrAsvcs(
                        current.leftServices.ssrAsvcs.begin() + 1, current.leftServices.ssrAsvcs.end());
            return SplitMsgInfo { resultMsg, AllServices { current.leftServices.ssvcs, leftSsrAsvcs, current.leftServices.osis }};
        }
    }
    else {
        const aim::ASVC& asvc = boost::get<aim::ASVC>(nextService);
        if (HaveLinkedSsrs(Ssrs(current.leftServices.ssrAsvcs), asvc)) {
            return AddSsrAsvcsByKey(current, SsrAsvcLinkKey(asvc));
        }
        else {
            aim::OutMsg resultMsg = current.message;
            resultMsg.ssrAsvcs.push_back(asvc);
            const std::vector<boost::variant<aim::SSR, aim::ASVC> > leftSsrAsvcs(
                        current.leftServices.ssrAsvcs.begin() + 1, current.leftServices.ssrAsvcs.end());
            return SplitMsgInfo { resultMsg, AllServices { current.leftServices.ssvcs, leftSsrAsvcs, current.leftServices.osis }};
        }
    }
}

/* Важно сохранить порядок услуг в разделённых телеграммах.
 * Что шло первым в исходной телеграмме - должно идти первым и в разделённой.
 * Но при этом пары связанных SSR-ASVC должны отправляться в одной телеграмме.
 * Поскольку точной привязки телеграмма не хранит, будем отделять сразу
 * все SSR-ASVC по каждому коду SSR и пассажиро-сегменту.
 * Теоретически это может вызвать нарушение порядка услуг,
 * но конкретно такое нарушение не должно привести к ошибкам. */
static SplitMsgInfo AddNextServiceToMessage(const SplitMsgInfo& current)
{
    if (current.leftServices.ssvcs.size() > 0) {
        aim::OutMsg resultMsg = current.message;
        resultMsg.ssvcs.push_back(current.leftServices.ssvcs.at(0));
        const std::vector<aim::SSVC> leftSvcs(current.leftServices.ssvcs.begin() + 1, current.leftServices.ssvcs.end());
        return SplitMsgInfo { resultMsg, AllServices { leftSvcs, current.leftServices.ssrAsvcs, current.leftServices.osis } };
    }
    else if (current.leftServices.ssrAsvcs.size() > 0) {
        return AddNextSsrToMessage(current);
    }
    else {
        ASSERT(current.leftServices.osis.size() > 0);
        aim::OutMsg resultMsg = current.message;
        resultMsg.osis.push_back(current.leftServices.osis.at(0));
        const std::vector<aim::OSI> leftOsis(current.leftServices.osis.begin() + 1, current.leftServices.osis.end());
        return SplitMsgInfo { resultMsg, AllServices { current.leftServices.ssvcs, current.leftServices.ssrAsvcs, leftOsis } };
    }
}

static SplitMsgInfo MakeFirstSplitMessage(const aim::Message& origMessage, const AptInfo& aptInfo)
{
    SplitMsgInfo current = MakeMinimalFirstMessage(origMessage);
    if (MessageExceedsMaxLength(current.message, aptInfo)) {
        LogError(STDLOG) << __FUNCTION__ << " Can't split long message! Sending as is.";
        return SplitMsgInfo { origMessage.msg(), AllServices() };
    }
    while (!current.leftServices.Empty())
    {
        const SplitMsgInfo next = AddNextServiceToMessage(current);
        if (MessageExceedsMaxLength(next.message, aptInfo)) {
            return current;
        }
        else current = next;
    }
    return current;
}

static aim::CRef MakeSecondaryCref(const aim::CRef& origCref)
{
    aim::CRefData crefData = origCref.data();
    crefData.carf = boost::none;
    return aim::CRef(origCref.addr(), crefData);
}

static SplitMsgInfo MakeNextSplitMessage(
        const aim::Message& origMessage,
        const AptInfo& aptInfo,
        const AllServices& leftServices,
        const boost::optional<aim::SSR>& grpsSsr,
        const std::vector<aim::MsgId>& msgId,
        const aim::PassengerList& passList,
        const std::vector<aim::SegmentBase::ConstPtr_t>& segments)
{
    const aim::OutMsg baseMessage
    {
        origMessage.addressList(),
        MakeSecondaryCref(origMessage.cref()),
        msgId,
        origMessage.reclocs(),
        boost::optional<aim::PassengerList>(),
        passList,
        segments,
        std::vector<aim::Taxi>(),
        std::vector<aim::SSVC>(),
        grpsSsr ? std::vector<boost::variant<aim::SSR, aim::ASVC> >(1, *grpsSsr)
                : std::vector<boost::variant<aim::SSR, aim::ASVC> >(),
        std::vector<tbp::SSR>(),
        std::vector<aim::OSI>()
    };
    SplitMsgInfo current { baseMessage, leftServices };
    while (!current.leftServices.Empty())
    {
        const SplitMsgInfo next = AddNextServiceToMessage(current);
        if (MessageExceedsMaxLength(next.message, aptInfo))
        {
            if(current.leftServices.ssvcs.size() == leftServices.ssvcs.size()
            && current.leftServices.ssrAsvcs.size() == leftServices.ssrAsvcs.size()
            && current.leftServices.osis.size() == leftServices.osis.size())
            {
                // Ни одной услуги добавить не получилось - после первой же превысили размер телеграммы.
                // Падать может и не стоит, отправим телеграмму длиннее допустимого и попробуем делить дальше.
                LogError(STDLOG) << __FUNCTION__
                                 << " Can't properly split message, sending message with exceeding length: "
                                 << MakeMessageText(next.message, aptInfo).size();
                return next;
            }
            return current;
        }
        else current = next;
    }
    return current;
}

static aim::PassengerList GetPassListForSecondaryMessages(
        const aim::Message& origMessage,
        const AptInfo& aptInfo,
        const ipnr::Pnr& oldPnr,
        const ipnr::Pnr& newPnr,
        const boost::optional<EotrDivInfo>& pnrDivisionInfo)
{
    const bool notAllNamesPresent = algo::find_opt_if<boost::optional>(origMessage.osis(), [](const aim::OSI& osi)
    {
        // Наличие OSI TCP можно использовать как признак того, что в телеграмме перечислены не все пассажиры
        return IsTcpOsi(osi);
    }).is_initialized();
    if (notAllNamesPresent) {
        return MakeNamesToSendFromNewPnr(oldPnr, newPnr, pnrDivisionInfo, aptInfo.bcfg()->translit());
    }
    else return origMessage.passengers();
}

static STCODE ActionToStatusCode(const STCODE code)
{
    switch (code)
    {
    case SS_STAT : return IS_STAT;
    case TK_STAT : return HK_STAT;
    case TL_STAT : return HL_STAT;
    case KK_STAT : return HK_STAT;
    case US_STAT : return HL_STAT;
    case LL_STAT : return HL_STAT;//Вообще-то нужен IW_STAT, но у нас его пока нет
    case IN_STAT : return IN_STAT;
    case IS_STAT : return IS_STAT;
    case NN_STAT : return IN_STAT;
    case KL_STAT : return HK_STAT;
    case CN_STAT : return CQ_STAT;
    case HN_STAT : return HN_STAT;
    case TN_STAT : return HN_STAT;
    case TS_STAT : return HS_STAT;
    case DK_STAT : return HK_STAT;
    case CH_STAT : return CH_STAT;
    case CK_STAT : return CK_STAT;
    case CL_STAT : return CW_STAT;//см. LL_STAT
    case CQ_STAT : return CQ_STAT;
    case CS_STAT : return CH_STAT;
    case CU_STAT : return CW_STAT;
    case CW_STAT : return CW_STAT;
    default: return code;
    }
}

static std::vector<aim::SegmentBase::ConstPtr_t> GetSegmentsForSecondaryMessages(
        const aim::Message& origMessage,
        const unsigned newTotalNumberOfSeats)
{
    std::vector<aim::SegmentBase::ConstPtr_t> result;
    for (const aim::SegmentBase::ConstPtr_t segment : origMessage.segments())
    {
        if (aim::Segment::ConstPtr_t seg = std::dynamic_pointer_cast<const aim::Segment>(segment))
        {
            if (!IsCancelled(seg->actionCode())) {
                aim::Segment::Ptr_t newSeg = std::make_shared<aim::Segment>(*seg);
                newSeg->setStatus(ActionToStatusCode(newSeg->actionCode()));
                newSeg->setNumSeats(newTotalNumberOfSeats);
                result.push_back(newSeg);
            }
        }
        else result.push_back(segment);
    }
    return result;
}

std::vector<aim::OutMsg> SplitLongMessage(
        const aim::Message& origMessage,
        const AptInfo& aptInfo,
        const ipnr::Pnr& oldPnr,
        const ipnr::Pnr& newPnr,
        const boost::optional<EotrDivInfo>& pnrDivisionInfo)
{
    if (!aptInfo.cfg().splitLongMessages()
    || !MessageExceedsMaxLength(origMessage.msg(), aptInfo))
    {
        return std::vector<aim::OutMsg>(1, origMessage.msg());
    }
    LogTrace(TRACE1) << __FUNCTION__ << " Splitting long Airimp...";
    const boost::optional<aim::SSR> grpsSsr = FindGrpsSsr(origMessage.ssrAsvcs());
    const std::vector<aim::MsgId> msgIdForSecondaryMessages = origMessage.checkType(aim::MsgId::PDM) ?
                std::vector<aim::MsgId>(1, aim::MsgId::PDM) : std::vector<aim::MsgId>();
    const aim::PassengerList passListForSecondaryMessages =
            GetPassListForSecondaryMessages(origMessage, aptInfo, oldPnr, newPnr, pnrDivisionInfo);
    const std::vector<aim::SegmentBase::ConstPtr_t> segmentsForSecondaryMessages =
            GetSegmentsForSecondaryMessages(origMessage, passListForSecondaryMessages.TotalNumberOfSeats());

    SplitMsgInfo current = MakeFirstSplitMessage(origMessage, aptInfo);
    std::vector<aim::OutMsg> result = std::vector<aim::OutMsg>(1, current.message);
    while (!current.leftServices.Empty())
    {
        current = MakeNextSplitMessage(origMessage,
                                       aptInfo,
                                       current.leftServices,
                                       grpsSsr,
                                       msgIdForSecondaryMessages,
                                       passListForSecondaryMessages,
                                       segmentsForSecondaryMessages);
        result.push_back(current.message);
    }
    LogTrace(TRACE1) << __FUNCTION__ << " Split into " << result.size() << " messages";
    return result;
}

}//namespace libair
